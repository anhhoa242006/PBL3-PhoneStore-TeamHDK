using HDKmall.BLL.Interfaces;
using HDKmall.ViewModels;
using System.Security.Cryptography;
using System.Text;

namespace HDKmall.BLL.Services
{
    public class VNPayService : IVNPayService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<VNPayService> _logger;
        private readonly string _vnPayUrl;
        private readonly string _tmnCode;
        private readonly string _hashSecret;

        public VNPayService(IConfiguration configuration, ILogger<VNPayService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _vnPayUrl = _configuration["VNPay:Url"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            _tmnCode = _configuration["VNPay:TmnCode"] ?? "TESTMERCHANT";
            _hashSecret = _configuration["VNPay:HashSecret"] ?? "TESTSECRET";
        }

        public string CreatePaymentUrl(PaymentVM model, HttpContext context)
        {
            var vnTime = GetVietnamTime();
            var tick = vnTime.Ticks.ToString();
            var requestId = Random.Shared.Next(100000).ToString();

            var vnpayData = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", _tmnCode },
                { "vnp_Amount", ((long)model.TotalAmount * 100).ToString() },
                { "vnp_CreateDate", vnTime.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", GetIpAddress(context) },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", $"Thanh toan HD {model.OrderId}" },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", model.ReturnUrl },
                { "vnp_TxnRef", model.OrderCode },
                { "vnp_ExpireDate", vnTime.AddHours(1).ToString("yyyyMMddHHmmss") }
            };

            var queryBuilder = new StringBuilder();
            foreach (var item in vnpayData)
            {
                queryBuilder.AppendFormat("&{0}={1}", System.Net.WebUtility.UrlEncode(item.Key), System.Net.WebUtility.UrlEncode(item.Value));
            }

            var queryString = queryBuilder.ToString();
            if (queryString.Length > 0 && queryString.StartsWith("&"))
            {
                queryString = queryString.Substring(1);
            }
            var secureHash = HmacSHA512(_hashSecret, queryString);
            var paymentUrl = $"{_vnPayUrl}?{queryString}&vnp_SecureHash={secureHash}";

            _logger.LogInformation("VNPay payment URL created for order {OrderId}, txnRef={TxnRef}", model.OrderId, model.OrderCode);
            return paymentUrl;
        }

        public VNPaymentResponseVM PaymentExecute(IQueryCollection collections)
        {
            var vnpayData = new SortedDictionary<string, string>(StringComparer.Ordinal);
            var responseCode = "";
            var amount = 0L;
            var transactionNo = "";

            foreach (var key in collections.Keys)
            {
                if (key.StartsWith("vnp_"))
                {
                    vnpayData.Add(key, collections[key].ToString());
                }
            }

            if (vnpayData.ContainsKey("vnp_ResponseCode"))
                responseCode = vnpayData["vnp_ResponseCode"];
            if (vnpayData.ContainsKey("vnp_Amount"))
                long.TryParse(vnpayData["vnp_Amount"], out amount);
            if (vnpayData.ContainsKey("vnp_TransactionNo"))
                transactionNo = vnpayData["vnp_TransactionNo"];

            // Lấy chữ ký nhận được rồi xoá khỏi data trước khi tính lại
            var receivedHash = collections["vnp_SecureHash"].ToString();
            vnpayData.Remove("vnp_SecureHash");
            vnpayData.Remove("vnp_SecureHashType");

            var queryString = "";
            foreach (var item in vnpayData)
            {
                queryString += "&" + System.Net.WebUtility.UrlEncode(item.Key) + "=" + System.Net.WebUtility.UrlEncode(item.Value);
            }

            if (queryString.Length > 0 && queryString.StartsWith("&"))
            {
                queryString = queryString.Substring(1);
            }

            // Verify HMAC SHA512 chữ ký
            var computedHash = HmacSHA512(_hashSecret, queryString);
            var signatureValid = string.Equals(computedHash, receivedHash, StringComparison.OrdinalIgnoreCase);
            var isSuccess = signatureValid && responseCode == "00";

            if (!signatureValid)
            {
                _logger.LogWarning("VNPay signature mismatch. Computed={Computed}, Received={Received}", computedHash, receivedHash);
            }
            else if (responseCode != "00")
            {
                _logger.LogWarning("VNPay response code indicates failure: {ResponseCode}", responseCode);
            }
            else
            {
                _logger.LogInformation("VNPay signature valid, responseCode=00, transactionNo={TransactionNo}", transactionNo);
            }

            return new VNPaymentResponseVM
            {
                Success = isSuccess,
                Message = isSuccess ? "Thanh toán thành công" : $"Thanh toán thất bại (mã {responseCode})",
                // OrderId trong TxnRef có dạng "ORDER-{id}-{ticks}", không parse trực tiếp được;
                // VnPayReturn sẽ lấy orderId từ URL query param thay vì từ đây
                OrderId = 0,
                TransactionId = transactionNo,
                Amount = amount / 100m,
                TransactionDate = GetVietnamTime()
            };
        }

        private DateTime GetVietnamTime()
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"));
            }
        }

        private string GetIpAddress(HttpContext context)
        {
            var ipAddress = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault() ??
                           context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
                           context.Connection.RemoteIpAddress?.ToString();

            return ipAddress ?? "127.0.0.1";
        }

        private string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(inputData));
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }
    }
}

using HDKmall.BLL.Interfaces;
using HDKmall.ViewModels;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HDKmall.BLL.Services
{
    public class MoMoService : IMoMoService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MoMoService> _logger;
        private readonly string _momoUrl;
        private readonly string _partnerCode;
        private readonly string _accessKey;
        private readonly string _secretKey;

        public MoMoService(IConfiguration configuration, ILogger<MoMoService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _momoUrl = _configuration["MoMo:Url"] ?? "https://test-payment.momo.vn/v2/gateway/api/create";
            _partnerCode = _configuration["MoMo:PartnerCode"] ?? "MOMOBKUN20180529";
            _accessKey = _configuration["MoMo:AccessKey"] ?? "klm05TvNBzhg7h7j";
            _secretKey = _configuration["MoMo:SecretKey"] ?? "at67qH6mk8w5Y1nAyMoYKMWACiEi2bsa";
        }

        public async Task<string> CreatePaymentUrl(PaymentVM model, HttpContext context)
        {
            var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var requestId = Guid.NewGuid().ToString();
            var ipnUrl = model.IpnUrl ?? model.ReturnUrl; // fallback

            var rawData = $"accessKey={_accessKey}&amount={(long)model.TotalAmount}" +
                         $"&extraData=&ipnUrl={ipnUrl}&orderId={model.OrderCode}" +
                         $"&orderInfo=Thanh toan don hang {model.OrderId}" +
                         $"&partnerCode={_partnerCode}&redirectUrl={model.ReturnUrl}" +
                         $"&requestId={requestId}&requestType=payWithMethod";

            var signature = ComputeHmacSHA256(rawData, _secretKey);

            var momoRequest = new
            {
                partnerCode = _partnerCode,
                requestId = requestId,
                amount = (long)model.TotalAmount,
                orderId = model.OrderCode,
                orderInfo = $"Thanh toan don hang {model.OrderId}",
                redirectUrl = model.ReturnUrl,
                ipnUrl = ipnUrl,
                requestType = "payWithMethod",
                extraData = "",
                lang = "vi",
                signature = signature
            };

            using (var client = new HttpClient())
            {
                try
                {
                    var json = JsonSerializer.Serialize(momoRequest);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(_momoUrl, content);

                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("MoMo API response: {Response}", responseContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonDoc = JsonDocument.Parse(responseContent);
                        var root = jsonDoc.RootElement;

                        if (root.TryGetProperty("payUrl", out var payUrl))
                        {
                            _logger.LogInformation("MoMo payment URL created for order {OrderId}, orderId={OrderCode}", model.OrderId, model.OrderCode);
                            return payUrl.GetString();
                        }
                        throw new Exception($"MoMo request failed. Response: {responseContent}");
                    }
                    else
                    {
                        throw new Exception($"MoMo API error ({response.StatusCode}): {responseContent}");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Lỗi kết nối MoMo: {ex.Message}");
                }
            }

            return "";
        }

        public async Task<VNPaymentResponseVM> PaymentExecute(IQueryCollection collections)
        {
            var orderId = collections["orderId"].ToString();
            var message = collections["message"].ToString();
            var responseCode = collections["resultCode"].ToString();
            var requestId = collections["requestId"].ToString();
            var amountStr = collections["amount"].ToString();
            var partnerCode = collections["partnerCode"].ToString();
            var orderInfo = collections["orderInfo"].ToString();
            var orderType = collections["orderType"].ToString();
            var transId = collections["transId"].ToString();
            var payType = collections["payType"].ToString();
            var responseTime = collections["responseTime"].ToString();
            var extraData = collections["extraData"].ToString();
            var receivedSignature = collections["signature"].ToString();

            // Kiểm tra chữ ký MoMo (HMAC SHA256)
            // Raw string theo thứ tự alphabet theo quy định của MoMo
            var rawSignature =
                $"accessKey={_accessKey}" +
                $"&amount={amountStr}" +
                $"&extraData={extraData}" +
                $"&message={message}" +
                $"&orderId={orderId}" +
                $"&orderInfo={orderInfo}" +
                $"&orderType={orderType}" +
                $"&partnerCode={partnerCode}" +
                $"&payType={payType}" +
                $"&requestId={requestId}" +
                $"&responseTime={responseTime}" +
                $"&resultCode={responseCode}" +
                $"&transId={transId}";

            var computedSignature = ComputeHmacSHA256(rawSignature, _secretKey);
            var signatureValid = string.Equals(computedSignature, receivedSignature, StringComparison.OrdinalIgnoreCase);

            if (!signatureValid)
            {
                _logger.LogWarning(
                    "MoMo signature mismatch for orderId={OrderId}. Computed={Computed}, Received={Received}",
                    orderId, computedSignature, receivedSignature);
                return new VNPaymentResponseVM
                {
                    Success = false,
                    Message = "Chữ ký MoMo không hợp lệ.",
                    OrderId = 0,
                    TransactionId = transId,
                    Amount = 0,
                    TransactionDate = DateTime.Now
                };
            }

            var isSuccess = responseCode == "0";

            if (!isSuccess)
            {
                _logger.LogWarning("MoMo payment failed for orderId={OrderId}, resultCode={ResultCode}, message={Message}",
                    orderId, responseCode, message);
            }
            else
            {
                _logger.LogInformation("MoMo payment successful for orderId={OrderId}, transId={TransId}", orderId, transId);
            }

            return new VNPaymentResponseVM
            {
                Success = isSuccess,
                Message = isSuccess ? "Thanh toán thành công" : $"Thanh toán thất bại: {message}",
                OrderId = 0, // sẽ được parse từ OrderCode trong MoMoReturn
                TransactionId = transId,
                Amount = string.IsNullOrEmpty(amountStr) ? 0 : decimal.Parse(amountStr),
                TransactionDate = DateTime.Now
            };
        }

        private string GetIpAddress(HttpContext context)
        {
            var ipAddress = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault() ??
                           context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
                           context.Connection.RemoteIpAddress?.ToString();

            return ipAddress ?? "127.0.0.1";
        }

        private string ComputeHmacSHA256(string message, string secretKey)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                var hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                return BitConverter.ToString(hashValue).Replace("-", "").ToLower();
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace StreamCore.Filter
{
    public class AuthonizationFilter : Attribute, IAuthorizationFilter
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        // 添加白名单
        private readonly List<string> _whiteList = new() { "Login" };
        public AuthonizationFilter(IConfiguration configuration, ILogger<AuthonizationFilter> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }


        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // 1️⃣ 获取当前 Action 名称
            var actionName = context.ActionDescriptor.RouteValues["action"];
            _logger.LogInformation($"请求进入过滤器，Action: {actionName}");
            // 2️⃣ 白名单检查：如果是登录接口，直接放行
            if (_whiteList.Contains(actionName, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogInformation("白名单接口，跳过 Token 验证");
                return;
            }
            // 3️⃣ 从请求头获取 Authorization 值
            var authorizationHeader = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authorizationHeader))
            {
                _logger.LogWarning("请求头中缺少 Authorization");
                context.Result = new UnauthorizedObjectResult(new { success = false, message = "缺少 Token" });
                return;
            }
            // 4️⃣ 提取 Token：兼容两种格式
            string token = null;
            if (authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authorizationHeader.Substring("Bearer ".Length).Trim();
            }
            else
            {
                // 如果没有 Bearer 前缀，则认为整个 header 就是 token
                token = authorizationHeader.Trim();
            }

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Authorization 头格式不正确，无法提取 Token");
                context.Result = new UnauthorizedObjectResult(new { success = false, message = "Token 格式错误" });
                return;
            }

            // 5️⃣ 验证 JWT Token
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var secretKey = _configuration["Jwt:SecretKey"];
                if (string.IsNullOrEmpty(secretKey))
                {
                    throw new InvalidOperationException("JWT SecretKey 未配置");
                }

                var key = Encoding.UTF8.GetBytes(secretKey);

                // 从配置读取 Issuer 和 Audience（生产环境必须验证）
                var validIssuer = _configuration["Jwt:Issuer"];
                var validAudience = _configuration["Jwt:Audience"];

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = !string.IsNullOrEmpty(validIssuer),
                    ValidIssuer = validIssuer,
                    ValidateAudience = !string.IsNullOrEmpty(validAudience),
                    ValidAudience = validAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero   // 严格控制过期时间
                };

                tokenHandler.ValidateToken(token, validationParameters, out _);
                _logger.LogInformation("Token 验证通过");
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("Token 已过期");
                context.Result = new UnauthorizedObjectResult(new { success = false, message = "Token 已过期" });
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                _logger.LogWarning("Token 签名无效");
                context.Result = new UnauthorizedObjectResult(new { success = false, message = "Token 无效" });
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Token 验证失败: {ex.Message}");
                context.Result = new UnauthorizedObjectResult(new { success = false, message = "Token 无效或已过期" });
            }
        }
    }
}
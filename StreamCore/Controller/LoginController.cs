using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace StreamCore.Controller
{
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        [HttpGet]
        [Route("api/shipping/Login")]
        public IActionResult Login(string username, string password)
        {
            // 参数校验
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return BadRequest(new { success = false, message = "用户名和密码不能为空" });
            }
            if (username == "admin" && password == "123")
            {
                // 生成 JWT Token
                var token = GenerateJwtToken(username);
                return Ok(new
                {
                    success = true,
                    message = "登录成功",
                    token = token,          // 返回 token 给客户端
                    data = new { account = username }
                });
            }
            else
            {
                return Unauthorized(new { success = false, message = "用户名或密码错误" });
            }

        }
        /// <summary>
        /// 生成 JWT Token
        /// </summary>
        private string GenerateJwtToken(string account)
        {
            // 从配置文件读取 JWT 配置
            var jwtConfig = _configuration.GetSection("Jwt");
            var secretKey = jwtConfig["SecretKey"];
            var issuer = jwtConfig["Issuer"];
            var audience = jwtConfig["Audience"];
            var expireMinutes = Convert.ToDouble(jwtConfig["ExpireMinutes"] ?? "120");
            // 安全密钥（长度至少 32 字节）
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            // 自定义声明（可包含用户信息）
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, account),
                new Claim(ClaimTypes.Role, "User"),           // 示例角色
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            // 创建 Token
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

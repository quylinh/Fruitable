using System.Linq;
using System.Web.Mvc;
using System.Security.Cryptography;
using System.Text;
using doantotnghiep.Models;
using System;
using System.Configuration;
using System.Web.Security;
using System.Web;

namespace doantotnghiep.Controllers
{
    public class LoginSignupController : Controller
    {
        readonly FreshFruitDBEntities db = new FreshFruitDBEntities();

        // Tạo salt ngẫu nhiên
        private string GenerateSalt()
        {
            byte[] saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        // Hàm mã hóa mật khẩu với salt
        private string EncryptPassword(string password, string salt)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                string saltedPassword = password + salt;
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // Lưu token vào Cache thay vì Session để tránh mất dữ liệu
        private void StoreActivationToken(string token, string username)
        {
            // Sử dụng HttpRuntime.Cache thay vì Session
            HttpRuntime.Cache.Insert(
                "ActivationToken_" + token,
                username,
                null,
                DateTime.Now.AddHours(24), // Absolute expiration
                TimeSpan.Zero // No sliding expiration
            );

            HttpRuntime.Cache.Insert(
                "ActivationTokenExpire_" + token,
                DateTime.Now.AddHours(24),
                null,
                DateTime.Now.AddHours(24),
                TimeSpan.Zero
            );
        }

        private bool IsActivationTokenValid(string token, out string username)
        {
            username = null;

            if (string.IsNullOrEmpty(token))
            {
                System.Diagnostics.Debug.WriteLine("Token is null or empty");
                return false;
            }

            // Kiểm tra trong Cache
            var storedUsername = HttpRuntime.Cache["ActivationToken_" + token];
            var expireTime = HttpRuntime.Cache["ActivationTokenExpire_" + token];

            System.Diagnostics.Debug.WriteLine($"Token: {token}");
            System.Diagnostics.Debug.WriteLine($"Stored username: {storedUsername}");
            System.Diagnostics.Debug.WriteLine($"Expire time: {expireTime}");

            if (storedUsername == null || expireTime == null)
            {
                System.Diagnostics.Debug.WriteLine("Token not found in cache");
                return false;
            }

            DateTime expire = (DateTime)expireTime;
            if (DateTime.Now > expire)
            {
                System.Diagnostics.Debug.WriteLine("Token expired");
                // Xóa token đã hết hạn
                HttpRuntime.Cache.Remove("ActivationToken_" + token);
                HttpRuntime.Cache.Remove("ActivationTokenExpire_" + token);
                return false;
            }

            username = storedUsername.ToString();
            System.Diagnostics.Debug.WriteLine($"Token valid for user: {username}");
            return true;
        }

        // Hiển thị form đăng ký và đăng nhập
        public ActionResult Index()
        {
            return View();
        }

        // Xử lý đăng ký
        [HttpPost]
        public ActionResult DangKy(Account_KhachHang account, string ConfirmPassword)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (account.MatKhau == ConfirmPassword)
                    {
                        var existingAccount = db.Account_KhachHang.SingleOrDefault(x => x.TaiKhoan == account.TaiKhoan);
                        if (existingAccount == null)
                        {
                            // Tạo salt và mã hóa mật khẩu
                            string salt = GenerateSalt();
                            account.MatKhau = EncryptPassword(account.MatKhau, salt);
                            account.Salt = salt;

                            account.TenKH = string.IsNullOrEmpty(account.TenKH) ? "Khách hàng mới" : account.TenKH;
                            account.Diachi = string.IsNullOrEmpty(account.Diachi) ? "Chưa cập nhật" : account.Diachi;
                            account.Email = string.IsNullOrEmpty(account.Email) ? "chua@capnhat.com" : account.Email;

                            // Đặt trạng thái chưa kích hoạt
                            account.IsActivated = false;

                            DateTime parsedDate;
                            if (!DateTime.TryParse(Request.Form["Ngaysinh"], out parsedDate))
                            {
                                account.Ngaysinh = new DateTime(2000, 1, 1);
                            }
                            else
                            {
                                account.Ngaysinh = parsedDate;
                            }

                            db.Account_KhachHang.Add(account);
                            db.SaveChanges();

                            // Gửi email xác thực
                            string activationToken = Guid.NewGuid().ToString();
                            StoreActivationToken(activationToken, account.TaiKhoan);

                            System.Diagnostics.Debug.WriteLine($"Created token: {activationToken} for user: {account.TaiKhoan}");

                            try
                            {
                                SendActivationEmail(account.Email, account.TaiKhoan, activationToken);
                                ViewBag.SuccessMessage = "Đăng ký thành công! Vui lòng kiểm tra email để kích hoạt tài khoản.";
                            }
                            catch (Exception emailEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Email error: {emailEx.Message}");
                                ViewBag.SuccessMessage = "Đăng ký thành công! Tuy nhiên có lỗi khi gửi email. Vui lòng liên hệ admin.";
                            }

                            return View("Index");
                        }
                        else
                        {
                            ViewBag.Error = "Tài khoản đã tồn tại. Vui lòng chọn tài khoản khác.";
                            return View("Index");
                        }
                    }
                    else
                    {
                        ViewBag.Error = "Mật khẩu không khớp. Vui lòng kiểm tra lại.";
                        return View("Index");
                    }
                }

                ViewBag.Error = "Có lỗi khi đăng ký, vui lòng kiểm tra lại thông tin.";
                return View("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Registration error: {ex.Message}");
                ViewBag.Error = "Có lỗi xảy ra: " + ex.Message;
                return View("Index");
            }
        }

        // Gửi email kích hoạt tài khoản
        private void SendActivationEmail(string toEmail, string username, string token)
        {
            string fromEmail = ConfigurationManager.AppSettings["FromEmail"];
            string appPassword = ConfigurationManager.AppSettings["AppPassword"];

            string activationLink = Url.Action("KichHoatTaiKhoan", "LoginSignup", new { token = token }, protocol: Request.Url.Scheme);

            System.Diagnostics.Debug.WriteLine($"Activation link: {activationLink}");

            string subject = "Kích hoạt tài khoản FreshFruit";
            string body = $"<p>Xin chào <strong>{username}</strong>,</p>" +
                          $"<p>Cảm ơn bạn đã đăng ký tài khoản tại hệ thống FreshFruit.</p>" +
                          $"<p>Để hoàn tất quá trình đăng ký, vui lòng nhấn vào liên kết dưới đây để kích hoạt tài khoản:</p>" +
                          $"<p><a href='{activationLink}' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Kích hoạt tài khoản</a></p>" +
                          $"<p>Hoặc copy và dán liên kết sau vào trình duyệt:</p>" +
                          $"<p>{activationLink}</p>" +
                          $"<p><strong>Lưu ý:</strong> Liên kết này sẽ hết hạn sau 24 giờ.</p>" +
                          $"<p>Nếu bạn không thực hiện đăng ký này, vui lòng bỏ qua email.</p>" +
                          $"<p>Trân trọng,<br/>Đội ngũ FreshFruit</p>";

            using (var smtp = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587))
            {
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new System.Net.NetworkCredential(fromEmail, appPassword);

                var message = new System.Net.Mail.MailMessage();
                message.From = new System.Net.Mail.MailAddress(fromEmail, "FreshFruit Support");
                message.To.Add(toEmail);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                smtp.Send(message);
            }
        }

        // Kích hoạt tài khoản
        public ActionResult KichHoatTaiKhoan(string token)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Activation attempt with token: {token}");

                string taiKhoan;
                if (!IsActivationTokenValid(token, out taiKhoan))
                {
                    ViewBag.Error = "Liên kết kích hoạt không hợp lệ hoặc đã hết hạn. Vui lòng thử gửi lại email kích hoạt.";
                    return View("Index");
                }

                var user = db.Account_KhachHang.SingleOrDefault(x => x.TaiKhoan == taiKhoan);

                if (user == null)
                {
                    ViewBag.Error = "Không tìm thấy tài khoản.";
                    return View("Index");
                }

                if (user.IsActivated == true)
                {
                    ViewBag.SuccessMessage = "Tài khoản đã được kích hoạt trước đó. Bạn có thể đăng nhập ngay.";
                    return View("Index");
                }

                // Kích hoạt tài khoản
                user.IsActivated = true;
                db.SaveChanges();

                // Xóa token khỏi cache
                HttpRuntime.Cache.Remove("ActivationToken_" + token);
                HttpRuntime.Cache.Remove("ActivationTokenExpire_" + token);

                System.Diagnostics.Debug.WriteLine($"Account activated successfully for user: {taiKhoan}");

                ViewBag.SuccessMessage = "Kích hoạt tài khoản thành công! Bạn có thể đăng nhập ngay bây giờ.";
                return View("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Activation error: {ex.Message}");
                ViewBag.Error = "Có lỗi xảy ra khi kích hoạt tài khoản: " + ex.Message;
                return View("Index");
            }
        }

        // Xử lý đăng nhập (cập nhật để kiểm tra tài khoản đã kích hoạt)
        [HttpPost]
        public ActionResult DangNhap(string TaiKhoan, string MatKhau)
        {
            try
            {
                var accountCheck = db.Account_KhachHang.SingleOrDefault(x => x.TaiKhoan == TaiKhoan);

                if (accountCheck != null)
                {
                    if (accountCheck.IsActivated != true)
                    {
                        ViewBag.LoginFail = "Tài khoản chưa được kích hoạt. Vui lòng kiểm tra email và kích hoạt tài khoản trước khi đăng nhập.";
                        return View("Index");
                    }

                    string matKhauMaHoa = EncryptPassword(MatKhau, accountCheck.Salt);

                    if (accountCheck.MatKhau == matKhauMaHoa)
                    {
                        Session["UserID"] = accountCheck.AccountID;
                        Session["UserName"] = accountCheck.TaiKhoan;
                        Session["FullName"] = accountCheck.TenKH;

                        FormsAuthentication.SetAuthCookie(accountCheck.TaiKhoan, false);

                        if (accountCheck.TaiKhoan == "admin")
                        {
                            Session["Role"] = "Admin";
                            return RedirectToAction("Index", "AdminHome", new { area = "Admin" });
                        }

                        Session["Role"] = "KhachHang";
                        return RedirectToAction("Index", "Home");
                    }
                }

                ViewBag.LoginFail = "Đăng nhập thất bại, vui lòng kiểm tra lại tài khoản và mật khẩu.";
                return View("Index");
            }
            catch (Exception ex)
            {
                ViewBag.LoginFail = "Có lỗi xảy ra: " + ex.Message;
                return View("Index");
            }
        }

        // Gửi lại email kích hoạt
        public ActionResult GuiLaiEmailKichHoat()
        {
            return View();
        }

        [HttpPost]
        public ActionResult GuiLaiEmailKichHoat(string TaiKhoan)
        {
            try
            {
                var user = db.Account_KhachHang.SingleOrDefault(x => x.TaiKhoan == TaiKhoan);
                if (user == null)
                {
                    ViewBag.Error = "Tài khoản không tồn tại.";
                    return View();
                }

                if (user.IsActivated == true)
                {
                    ViewBag.Error = "Tài khoản đã được kích hoạt.";
                    return View();
                }

                // Tạo token mới
                string activationToken = Guid.NewGuid().ToString();
                StoreActivationToken(activationToken, user.TaiKhoan);

                SendActivationEmail(user.Email, user.TaiKhoan, activationToken);

                ViewBag.Success = "Đã gửi lại email kích hoạt. Vui lòng kiểm tra hộp thư của bạn.";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Có lỗi xảy ra: " + ex.Message;
                return View();
            }
        }

        // Các phương thức khác giữ nguyên...
        private void SendResetEmail(string toEmail, string username, string token)
        {
            string fromEmail = ConfigurationManager.AppSettings["FromEmail"];
            string appPassword = ConfigurationManager.AppSettings["AppPassword"];

            string resetLink = Url.Action("DatLaiMatKhau", "LoginSignup", new { token = token }, protocol: Request.Url.Scheme);
            string subject = "Đặt lại mật khẩu của bạn";
            string body = $"<p>Xin chào <strong>{username}</strong>,</p>" +
                          $"<p>Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản tại hệ thống FreshFruit.</p>" +
                          $"<p>Vui lòng nhấn vào liên kết dưới đây để đặt lại mật khẩu:</p>" +
                          $"<p><a href='{resetLink}'>Đặt lại mật khẩu</a></p>" +
                          $"<p>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email.</p>";

            using (var smtp = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587))
            {
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new System.Net.NetworkCredential(fromEmail, appPassword);

                var message = new System.Net.Mail.MailMessage();
                message.From = new System.Net.Mail.MailAddress(fromEmail, "FreshFruit Support");
                message.To.Add(toEmail);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                smtp.Send(message);
            }
        }

        public ActionResult QuenMatKhau()
        {
            return View();
        }

        [HttpPost]
        public ActionResult QuenMatKhau(string TaiKhoan)
        {
            var user = db.Account_KhachHang.SingleOrDefault(x => x.TaiKhoan == TaiKhoan);
            if (user != null)
            {
                string token = Guid.NewGuid().ToString();
                Session["ResetToken_" + token] = user.TaiKhoan;

                SendResetEmail(user.Email, user.TaiKhoan, token);

                ViewBag.Success = "Đã gửi liên kết đặt lại mật khẩu tới email của bạn.";
                return RedirectToAction("Index", "LoginSignup");
            }
            ViewBag.Error = "Tài khoản không tồn tại.";
            return View();
        }

        public ActionResult DatLaiMatKhau(string token)
        {
            if (string.IsNullOrEmpty(token) || Session["ResetToken_" + token] == null)
            {
                return View("Index");
            }

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        public ActionResult DatLaiMatKhau(string token, string MatKhauMoi, string XacNhanMatKhau)
        {
            if (string.IsNullOrEmpty(token) || Session["ResetToken_" + token] == null)
            {
                return RedirectToAction("QuenMatKhau");
            }

            string taiKhoan = Session["ResetToken_" + token].ToString();
            var user = db.Account_KhachHang.SingleOrDefault(x => x.TaiKhoan == taiKhoan);
            if (user == null)
            {
                ViewBag.Error = "Không tìm thấy tài khoản.";
                return View();
            }

            if (MatKhauMoi != XacNhanMatKhau)
            {
                ViewBag.Error = "Mật khẩu không khớp.";
                ViewBag.Token = token;
                return View();
            }

            string newSalt = GenerateSalt();
            user.MatKhau = EncryptPassword(MatKhauMoi, newSalt);
            user.Salt = newSalt;
            db.SaveChanges();

            Session.Remove("ResetToken_" + token);
            ViewBag.Success = "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập.";
            return View("Index");
        }

        public ActionResult Logout()
        {
            Session.Clear();
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }

        // Phương thức debug để kiểm tra token
        public ActionResult CheckToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { error = "Token is null or empty" }, JsonRequestBehavior.AllowGet);
            }

            var storedUsername = HttpRuntime.Cache["ActivationToken_" + token];
            var expireTime = HttpRuntime.Cache["ActivationTokenExpire_" + token];

            return Json(new
            {
                tokenExists = storedUsername != null,
                username = storedUsername?.ToString(),
                expireTime = expireTime?.ToString(),
                isExpired = expireTime != null && DateTime.Now > (DateTime)expireTime
            }, JsonRequestBehavior.AllowGet);
        }
    }
}
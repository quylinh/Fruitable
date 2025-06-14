using System.Linq;
using System.Web.Mvc;
using System.Security.Cryptography;
using System.Text;
using doantotnghiep.Models;
using System;
using System.Configuration;
using System.Web.Security;

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
                            account.Salt = salt; // Lưu salt vào database

                            account.TenKH = string.IsNullOrEmpty(account.TenKH) ? "Khách hàng mới" : account.TenKH;
                            account.Diachi = string.IsNullOrEmpty(account.Diachi) ? "Chưa cập nhật" : account.Diachi;
                            account.Email = string.IsNullOrEmpty(account.Email) ? "chua@capnhat.com" : account.Email;

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

                            ViewBag.SuccessMessage = "Đăng ký thành công. Bạn có thể đăng nhập ngay bây giờ.";
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
                ViewBag.Error = "Có lỗi xảy ra: " + ex.Message;
                return View("Index");
            }
        }

        // Xử lý đăng nhập
        [HttpPost]
        public ActionResult DangNhap(string TaiKhoan, string MatKhau)
        {
            try
            {
                // Lấy thông tin tài khoản từ database
                var accountCheck = db.Account_KhachHang.SingleOrDefault(x => x.TaiKhoan == TaiKhoan);

                if (accountCheck != null)
                {
                    // Sử dụng salt của tài khoản để mã hóa mật khẩu nhập vào
                    string matKhauMaHoa = EncryptPassword(MatKhau, accountCheck.Salt);

                    // So sánh mật khẩu đã mã hóa
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

        // Hiển thị form quên mật khẩu
        public ActionResult QuenMatKhau()
        {
            return View();
        }

        // POST: Xử lý form quên mật khẩu
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

        // GET: Hiển thị form đặt lại mật khẩu
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

            // Tạo salt mới và mã hóa mật khẩu mới
            string newSalt = GenerateSalt();
            user.MatKhau = EncryptPassword(MatKhauMoi, newSalt);
            user.Salt = newSalt;
            db.SaveChanges();

            Session.Remove("ResetToken_" + token);
            ViewBag.Success = "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập.";
            return View("Index");
        }
    }
}
using doantotnghiep.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace doantotnghiep.Controllers
{
    public class ShopDetailController : Controller
    {
        readonly FreshFruitDBEntities db = new FreshFruitDBEntities();

        public ActionResult Index(int? id)
        {
            // Lấy sản phẩm theo ID
            var sp = db.SanPhams.FirstOrDefault(s => s.MaSP == id);
            if (sp == null)
            {
                // Nếu không có sản phẩm theo ID, lấy sản phẩm đầu tiên
                sp = db.SanPhams.OrderBy(s => s.MaSP).FirstOrDefault();
                if (sp == null)
                {
                    return HttpNotFound("Không có sản phẩm nào.");
                }
            }

            // Lấy tên khách hàng nếu đã đăng nhập
            if (Session["UserID"] != null)
            {
                int userId = (int)Session["UserID"];
                var khachHang = db.Account_KhachHang.FirstOrDefault(kh => kh.AccountID == userId);
                if (khachHang != null)
                {
                    ViewBag.TenKhachHang = khachHang.TaiKhoan;  // Giả sử cột tên là TenKH
                }
            }

            return View(sp);
        }

        [HttpPost]
        public ActionResult ThemNhanXet(int maSP, string noiDung, int soSao)
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Index", "LoginSignup");
            }

            int userId = (int)Session["UserID"];

            // Kiểm tra khách hàng đã mua sản phẩm này chưa
            var daMua = db.ChiTietDonHangs.Any(ct =>
                ct.MaSP == maSP &&
                ct.DonHang.MaKH == userId &&
                ct.DonHang.TrangThai == "Hoàn thành");

            if (!daMua)
            {
                TempData["error"] = "Bạn chỉ có thể đánh giá sau khi đã mua sản phẩm.";
                return RedirectToAction("Index", new { id = maSP });
            }

            // Tạo nhận xét mới
            var nx = new NhanXetSanPham
            {
                MaSP = maSP,
                MaKH = userId,
                NoiDung = noiDung,
                SoSao = soSao,
                NgayNhanXet = DateTime.Now
            };

            db.NhanXetSanPhams.Add(nx);
            db.SaveChanges();

            TempData["success"] = "Cảm ơn bạn đã đánh giá!";
            return RedirectToAction("Index", new { id = maSP });
        }
    }
}

/*using System.Linq;
using System.Web.Mvc;
using doantotnghiep.Models;
using System;

namespace doantotnghiep.Areas.Admin.Controllers
{
    public class AdminHomeController : Controller
    {
        FreshFruitDBEntities db = new FreshFruitDBEntities();

        public ActionResult Index()
        {
            if (Session["UserID"] == null || Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Index", "LoginSignup", new { area = "" });
            }

            var today = DateTime.Today;

            ViewBag.DoanhThuHomNay = db.DonHangs
                .Where(d => d.NgayTao == today)
                .Sum(d => (decimal?)d.TongTien) ?? 0;

            ViewBag.TongDoanhSo = db.DonHangs
                .Sum(d => (decimal?)d.TongTien) ?? 0;

            ViewBag.KhachHangMoi = db.Account_KhachHang
                .Count(kh => kh.Ngaysinh == today); // nếu có cột ngày tạo thì thay đổi

            ViewBag.SoNguoiDung = db.Account_KhachHang.Count();

            // ✅ Thêm phần này: lấy 5 đơn hàng mới nhất
            var donHangMoiNhat = db.DonHangs
                .OrderByDescending(d => d.NgayTao)
                .Take(5)
                .ToList();

            return View(donHangMoiNhat);
        }

    }
}
*/using System.Linq;
using System.Web.Mvc;
using doantotnghiep.Models;
using System;
using System.Data.Entity;

namespace doantotnghiep.Areas.Admin.Controllers
{
    public class AdminHomeController : Controller
    {
        FreshFruitDBEntities db = new FreshFruitDBEntities();

        public ActionResult Index()
        {
            if (Session["UserID"] == null || Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Index", "LoginSignup", new { area = "" });
            }

            var today = DateTime.Today;

            ViewBag.DoanhThuHomNay = db.DonHangs
                .Where(d => DbFunctions.TruncateTime(d.NgayTao) == today)
                .Sum(d => (decimal?)d.TongTien) ?? 0;

            ViewBag.TongDoanhSo = db.DonHangs
                .Sum(d => (decimal?)d.TongTien) ?? 0;

            ViewBag.KhachHangMoi = db.Account_KhachHang
                .Count(kh => DbFunctions.TruncateTime(kh.Ngaysinh) == today); // nếu có NgayTao thì đổi lại

            ViewBag.SoNguoiDung = db.Account_KhachHang.Count();

            var donHangMoiNhat = db.DonHangs
                .OrderByDescending(d => d.NgayTao)
                .Take(5)
                .ToList();

            return View(donHangMoiNhat);
        }
    }
}

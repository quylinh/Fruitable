using doantotnghiep.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;

namespace doantotnghiep.Controllers
{
    public class ShopDetailController : Controller
    {
        readonly FreshFruitDBEntities db = new FreshFruitDBEntities();

        public ActionResult Index(int? id)
        {
            var sp = db.SanPhams
                .Include(s => s.NhanXetSanPhams)
                .FirstOrDefault(s => s.MaSP == id);

            if (sp == null)
            {
                sp = db.SanPhams
                    .Include(s => s.NhanXetSanPhams)
                    .OrderBy(s => s.MaSP)
                    .FirstOrDefault();

                if (sp == null)
                {
                    return HttpNotFound("Không có sản phẩm nào.");
                }
            }

            ViewBag.SoLuongNhietDoi = db.SanPhams.Count(spItem => spItem.DanhMucSP.TenDM == "Nhiệt đới");
            ViewBag.SoLuongOnDoi = db.SanPhams.Count(spItem => spItem.DanhMucSP.TenDM == "Ôn đới");
            ViewBag.SoLuongCamQuyt = db.SanPhams.Count(spItem => spItem.DanhMucSP.TenDM == "Họ cam quýt");
            ViewBag.SoLuongNhapKhau = db.SanPhams.Count(spItem => spItem.DanhMucSP.TenDM == "Nhập khẩu");


            if (Session["UserID"] != null)
            {
                int userId = (int)Session["UserID"];
                var khachHang = db.Account_KhachHang.FirstOrDefault(kh => kh.AccountID == userId);
                if (khachHang != null)
                {
                    ViewBag.TenKhachHang = khachHang.TaiKhoan;
                }
            }

            return View(sp);
        }

        // Nếu dùng partial riêng cho sidebar thì gọi bằng Html.Partial
        public PartialViewResult DanhMucSidebar()
        {
            var danhMucs = db.DanhMucSPs
                .Select(dm => new {
                    TenDM = dm.TenDM,
                    MaDM = dm.MaDM,
                    SoLuongSP = db.SanPhams.Count(sp => sp.MaDM == dm.MaDM)
                }).ToList();

            ViewBag.DanhMucs = danhMucs;
            return PartialView();
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

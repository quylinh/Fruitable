using doantotnghiep.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace doantotnghiep.Areas.Admin.Controllers
{
    public class AdminSanPhamController : Controller
    {
        readonly FreshFruitDBEntities db = new FreshFruitDBEntities();

        // Hiển thị danh sách sản phẩm
        public ActionResult Index()
        {
            if (Session["UserID"] == null || Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Index", "LoginSignup", new { area = "" });
            }
            List<SanPham> dsSP = db.SanPhams.ToList();
            return View(dsSP);
        }

        // GET: Hiển thị form thêm sản phẩm
        public ActionResult Create()
        {
            // Lấy tất cả các danh mục từ bảng DanhMucSp
            ViewBag.MaDM = new SelectList(db.DanhMucSPs, "MaDM", "TenDM"); // MaDM là khóa chính, TenDM là tên danh mục

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(SanPham sp)
        {
            if (ModelState.IsValid)
            {
                db.SanPhams.Add(sp);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            // Truyền lại danh mục nếu có lỗi
            ViewBag.MaDM = new SelectList(db.DanhMucSPs, "MaDM", "TenDM");
            return View(sp);
        }


        // GET: Xem chi tiết sản phẩm
        public ActionResult Details(int id)
        {
            var sp = db.SanPhams.Find(id);
            if (sp == null)
                return HttpNotFound();
            return View(sp);
        }
        
        public ActionResult Delete(int id)
        {
            SanPham model = db.SanPhams.Find(id);
            return View(model);
        }
        // POST: Xác nhận và thực hiện xóa sản phẩm
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirm(int id)
        {
            var sp = db.SanPhams.Find(id);
            if (sp != null)
            {
                db.SanPhams.Remove(sp);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            SanPham model = db.SanPhams.Find(id);
            return View(model);
        }
        [HttpPost]

        public ActionResult Edit(SanPham model)
        {
            var updateModel = db.SanPhams.Find(model.MaSP);
            updateModel.TenSP = model.TenSP;
            updateModel.AnhSP = model.AnhSP;
            updateModel.GiaSP = model.GiaSP;
            updateModel.TrangThai = model.TrangThai;
            updateModel.MotaSP = model.MotaSP;
            updateModel.NgayNhap = model.NgayNhap;
            updateModel.NgayHuy = model.NgayHuy;

            db.SaveChanges();
            return RedirectToAction("Index");

        }

    }
}

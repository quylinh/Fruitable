using doantotnghiep.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace doantotnghiep.Areas.Admin.Controllers
{
    public class AdminDanhMucController : Controller
    {
        // GET: Admin/AdminDanhMuc
        readonly FreshFruitDBEntities db = new FreshFruitDBEntities();

        public ActionResult Index()
        {
            if (Session["UserID"] == null || Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Index", "LoginSignup", new { area = "" });
            }
            List<DanhMucSP> danhMucSPs = db.DanhMucSPs.ToList();

            return View(danhMucSPs);

        }
        public ActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Create(DanhMucSP model)
        {
            db.DanhMucSPs.Add(model);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        public ActionResult Delete(int id)
        {
            if (id != null)
            {
                var danhMuc = db.DanhMucSPs.Find(id);
                if (danhMuc == null)
                {
                    return HttpNotFound();
                }

                return View(danhMuc); // Trả về View để hiển thị dữ liệu
            }
            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }

        // POST: Admin/Account/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var danhMuc = db.DanhMucSPs.Find(id);

            // Lấy danh sách sản phẩm thuộc danh mục này
            var sanPhams = db.SanPhams.Where(sp => sp.MaDM == id).ToList();

            // Xóa từng sản phẩm liên quan
            foreach (var sp in sanPhams)
            {
                db.SanPhams.Remove(sp);
            }

            // Sau khi xóa sản phẩm, xóa danh mục
            db.DanhMucSPs.Remove(danhMuc);
            db.SaveChanges();

            return RedirectToAction("Index");
        }
        public ActionResult Edit(int id)
        {
           DanhMucSP model = db.DanhMucSPs.Find(id);
            return View(model);
        }
        [HttpPost]
        public ActionResult Edit(DanhMucSP model)
        {
            var updateModel = db.DanhMucSPs.Find(model.MaDM);
            if (updateModel != null)
            {
                updateModel.TenDM = model.TenDM;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return HttpNotFound();
        }

    }
}



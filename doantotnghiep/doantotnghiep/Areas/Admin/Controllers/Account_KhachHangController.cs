using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using doantotnghiep.Models;

namespace doantotnghiep.Areas.Admin.Controllers
{
    public class Account_KhachHangController : Controller
    {
        readonly FreshFruitDBEntities db = new FreshFruitDBEntities();

        // GET: Admin/Account
        public ActionResult Index()
        {
            if (Session["UserID"] == null || Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Index", "LoginSignup", new { area = "" });
            }
            List<Account_KhachHang> danhSachKhachHang = db.Account_KhachHang.ToList();

            return View(danhSachKhachHang);
        }
        public ActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Create(Account_KhachHang model)
        {
            db.Account_KhachHang.Add(model);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Details(int id)
        {
            Account_KhachHang model = db.Account_KhachHang.Find(id);
            return View(model);

        }
        public ActionResult Edit(int id)
        {
            Account_KhachHang model = db.Account_KhachHang.Find(id);
            return View(model);
        }
        [HttpPost]

        public ActionResult Edit(Account_KhachHang model)
        {
            var updateModel = db.Account_KhachHang.Find(model.AccountID);
            updateModel.TenKH = model.TenKH;
            updateModel.Phone = model.Phone;
            updateModel.Diachi = model.Diachi;
            db.SaveChanges();
            return RedirectToAction("Index");

        }
        /*   public ActionResult Delete(int id)
           {
               var updateMode = db.Account_KhachHang.Find(id);
               db.Account_KhachHang.Remove(updateMode);
               db.SaveChanges();
               return RedirectToAction("Index");
           }*/
        // GET: Admin/Account/Delete/5
        public ActionResult Delete(int id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var account = db.Account_KhachHang.Find(id);
            if (account == null)
            {
                return HttpNotFound();
            }

            return View(account); // Trả về View để hiển thị dữ liệu
        }

        // POST: Admin/Account/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var account = db.Account_KhachHang.Find(id);
            db.Account_KhachHang.Remove(account);
            db.SaveChanges();
            return RedirectToAction("Index");
        }



    }
}
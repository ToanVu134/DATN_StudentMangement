using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DATN_StudentMangement.Models;

namespace DATN_StudentMangement.Controllers
{
    public class AdminController : Controller
    {
        private QuanLiSinhVien_SenikaaEntities db = new QuanLiSinhVien_SenikaaEntities();

        public ActionResult DangNhap()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangNhap(string MaAD, string MatkhauAD)
        {
            if (ModelState.IsValid)
            {
                var admin = db.Taikhoans.FirstOrDefault(a => a.MaAD == MaAD && a.MatkhauAD == MatkhauAD);

                if (admin != null)
                {
                    Session["AdminUser"] = admin.MaAD;
                    return RedirectToAction("Index", "MonHoc");
                }
                else
                {
                    ViewBag.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác.";
                }
            }
            return View();
        }

        public ActionResult DangXuat()
        {
            Session["AdminUser"] = null;
            return RedirectToAction("DangNhap", "Admin");
        }
    }
}
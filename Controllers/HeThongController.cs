using System;
using System.Linq;
using System.Web.Mvc;
using DATN_StudentMangement.Models;
using System.Security.Cryptography;
using System.Text;

namespace DATN_StudentMangement.Controllers
{
    public class HeThongController : Controller
    {
        private QuanLiSinhVien_SenikaaEntities db = new QuanLiSinhVien_SenikaaEntities();

        // Hàm băm mật khẩu chuẩn SHA256
        public static string GetSHA256(string str)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(str));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        [HttpGet]
        public ActionResult DangNhap()
        {
            Session.Clear();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangNhap(string tenDangNhap, string matKhau)
        {
            if (ModelState.IsValid)
            {
                string matKhauMaHoa = GetSHA256(matKhau);
                var tk = db.TaiKhoans.FirstOrDefault(a => a.TenDangNhap == tenDangNhap && a.MatKhau == matKhauMaHoa);

                if (tk != null)
                {
                    Session["Quyen"] = tk.Quyen;
                    Session["TenDangNhap"] = tk.TenDangNhap;
                    Session["MaTK"] = tk.Id;

                    if (tk.Quyen == "Admin")
                    {
                        Session["HoTen"] = "Quản trị viên";
                        return RedirectToAction("Index", "LopHP");
                    }
                    else if (tk.Quyen == "GiangVien")
                    {
                        var gv = db.GiangViens.FirstOrDefault(g => g.MaTK == tk.Id);
                        if (gv != null)
                        {
                            Session["MaGV"] = gv.MaGV;
                            Session["HoTen"] = gv.HoTen;
                        }
                        return RedirectToAction("Index", "LopHP");
                    }
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
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("DangNhap", "HeThong");
        }

        [HttpGet]
        public ActionResult DoiMatKhau()
        {
            if (Session["MaTK"] == null) return RedirectToAction("DangNhap", "HeThong");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DoiMatKhau(string matKhauCu, string matKhauMoi, string xacNhanMatKhau)
        {
            if (Session["MaTK"] == null) return RedirectToAction("DangNhap", "HeThong");

            int maTK = Convert.ToInt32(Session["MaTK"]);
            var tk = db.TaiKhoans.Find(maTK);

            if (tk == null) return RedirectToAction("DangNhap", "HeThong");

            string matKhauCuMaHoa = GetSHA256(matKhauCu);

            if (tk.MatKhau != matKhauCuMaHoa)
            {
                ViewBag.ErrorMessage = "Mật khẩu cũ không chính xác.";
                return View();
            }

            if (matKhauMoi != xacNhanMatKhau)
            {
                ViewBag.ErrorMessage = "Xác nhận mật khẩu mới không khớp.";
                return View();
            }

            tk.MatKhau = GetSHA256(matKhauMoi);
            db.SaveChanges();

            TempData["ThongBao"] = "Đổi mật khẩu thành công.";
            return RedirectToAction("Index", "LopHP");
        }
    }
}
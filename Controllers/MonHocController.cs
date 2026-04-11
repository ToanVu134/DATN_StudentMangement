using System;
using System.Linq;
using System.Web.Mvc;
using DATN_StudentMangement.Models;

namespace DATN_StudentMangement.Controllers
{
    public class MonHocController : Controller
    {
        private QuanLiSinhVien_SenikaaEntities db = new QuanLiSinhVien_SenikaaEntities();

        public ActionResult Index()
        {
            if (Session["AdminUser"] == null)
            {
                return RedirectToAction("DangNhap", "Admin");
            }

            // Tính toán và truyền mã môn học tiếp theo sang View
            ViewBag.NextMaMH = GenerateNewMaMH();

            var danhSach = db.Monhocs.ToList();
            return View(danhSach);
        }

        private string GenerateNewMaMH()
        {
            var lastMonHoc = db.Monhocs.OrderByDescending(m => m.MaMH).FirstOrDefault();

            if (lastMonHoc == null || string.IsNullOrEmpty(lastMonHoc.MaMH))
            {
                return "MH001";
            }

            string lastId = lastMonHoc.MaMH;
            string digits = new string(lastId.Where(char.IsDigit).ToArray());

            if (string.IsNullOrEmpty(digits))
            {
                return lastId + "1";
            }

            string prefix = lastId.Substring(0, lastId.Length - digits.Length);

            if (int.TryParse(digits, out int number))
            {
                number++;
                return prefix + number.ToString("D" + digits.Length);
            }

            return lastId + "_new";
        }

        [HttpPost]
        public ActionResult LuuThongTin(Monhoc model)
        {
            var monHoc = db.Monhocs.FirstOrDefault(m => m.MaMH == model.MaMH);

            if (monHoc != null)
            {
                // Đã tồn tại mã này -> Thực hiện cập nhật
                monHoc.TenMH = model.TenMH;
                monHoc.SoTinChi = model.SoTinChi;
            }
            else
            {
                // Chưa tồn tại -> Thực hiện thêm mới. 
                // Gọi lại GenerateNewMaMH() để đảm bảo mã luôn mới nhất, phòng trường hợp có nhiều người dùng thao tác cùng lúc
                model.MaMH = GenerateNewMaMH();
                db.Monhocs.Add(model);
            }

            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Xoa(string id)
        {
            var monHoc = db.Monhocs.FirstOrDefault(m => m.MaMH == id);
            if (monHoc != null)
            {
                db.Monhocs.Remove(monHoc);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}
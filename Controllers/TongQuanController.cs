using System.Linq;
using System.Web.Mvc;
using DATN_StudentMangement.Models;

namespace DATN_StudentMangement.Controllers
{
    public class TongQuanController : Controller
    {
        private QuanLiSinhVien_SenikaaEntities db = new QuanLiSinhVien_SenikaaEntities();

        public ActionResult Index()
        {
            if (Session["Quyen"] == null) return RedirectToAction("DangNhap", "HeThong");

            var thongKeTrangThai = db.Sinhviens
                .GroupBy(s => s.TrangThaiHocTap)
                .Select(g => new { Ten = g.Key, SoLuong = g.Count() })
                .ToList();

            ViewBag.LabelsTrangThai = thongKeTrangThai.Select(x => string.IsNullOrEmpty(x.Ten) ? "Khác" : x.Ten).ToArray();
            ViewBag.DataTrangThai = thongKeTrangThai.Select(x => x.SoLuong).ToArray();

            var thongKeNganh = db.Sinhviens
                .GroupBy(s => s.NganhHoc)
                .Select(g => new { Ten = g.Key, SoLuong = g.Count() })
                .ToList();

            ViewBag.LabelsNganh = thongKeNganh.Select(x => string.IsNullOrEmpty(x.Ten) ? "Khác" : x.Ten).ToArray();
            ViewBag.DataNganh = thongKeNganh.Select(x => x.SoLuong).ToArray();

            var thongKeKhoa = db.Sinhviens
                .GroupBy(s => s.KhoaNhapHoc)
                .Select(g => new { Ten = g.Key, SoLuong = g.Count() })
                .OrderBy(x => x.Ten)
                .ToList();

            ViewBag.LabelsKhoa = thongKeKhoa.Select(x => string.IsNullOrEmpty(x.Ten) ? "Khác" : x.Ten).ToArray();
            ViewBag.DataKhoa = thongKeKhoa.Select(x => x.SoLuong).ToArray();

            ViewBag.TongSinhVien = db.Sinhviens.Count();
            ViewBag.TongGiangVien = db.GiangViens.Count();
            ViewBag.TongLopHP = db.LopHPs.Count(l => l.TrangThai != "Đã khóa");

            return View();
        }
    }
}
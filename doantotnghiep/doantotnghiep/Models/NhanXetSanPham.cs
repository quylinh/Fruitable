//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace doantotnghiep.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class NhanXetSanPham
    {
        public int MaNX { get; set; }
        public int MaKH { get; set; }
        public int MaSP { get; set; }
        public string NoiDung { get; set; }
        public Nullable<int> SoSao { get; set; }
        public Nullable<System.DateTime> NgayNhanXet { get; set; }
    
        public virtual Account_KhachHang Account_KhachHang { get; set; }
        public virtual SanPham SanPham { get; set; }
    }
}

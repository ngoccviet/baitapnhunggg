using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using code_web_nhung.Models;
using Microsoft.Data.SqlClient;
using System.Net.NetworkInformation;

namespace code_web_nhung.Controllers;

public class HomeController : Controller
{
    private readonly string _connectionString;
    private string connectionString = "Data Source=DESKTOP-PT451IS\\SQLEXPRESS;Initial Catalog=HeThongDieuKhien;Integrated Security=True;Trust Server Certificate=True";

    public HomeController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }
    [HttpGet]
    public IActionResult login()
    {
        return View(); // Trả về Views/Account/login.cshtml
    }
    [HttpPost]
    /////////////////////////////////////////////////////////////////////xử lý hàm đăng nhập 
    public IActionResult login(string username, string password)
    {
        // Kiểm tra đăng nhập trong database

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT COUNT(*) FROM dbo.dangnhap WHERE tentaikhoan = @tentaikhoan AND matkhau = @matkhau";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@tentaikhoan", username);
                command.Parameters.AddWithValue("@matkhau", password);

                object result = command.ExecuteScalar();
                if (result != null && Convert.ToInt32(result) > 0)
                {
                    // Đăng nhập thành công
                    return RedirectToAction("mainprogram");
                }
            }
        }

        ViewData["UsernameError"] = "Tài khoản hoặc mật khẩu không đúng!";
        ViewData["PasswordError"] = "Tài khoản hoặc mật khẩu không đúng!";
        ViewData["EnteredUsername"] = username;
        return View();
    }
    public IActionResult register()
    {
        return View(); // Trả về Views/Account/register.cshtml
    }
    [HttpPost]
    ///////////////////////////////////////////////////////////////////xử lý hàm đăng kí
    public IActionResult Register(string username, string password, string confirmPassword)
    {
        ViewData["EnteredUsername"] = username;
        if (password != confirmPassword)
        {
            ViewData["PasswordError"] = "Mật khẩu không khớp!";
            //ViewData["EnteredUsername"] = username;
            return View();
        }

        // Kiểm tra username đã tồn tại chưa
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            string checkQuery = "SELECT COUNT(*) FROM dbo.dangnhap WHERE tentaikhoan = @tentaikhoan";
            using (SqlCommand checkCmd = new SqlCommand(checkQuery, connection))
            {
                checkCmd.Parameters.AddWithValue("@tentaikhoan", username);
                int existingUser = (int)checkCmd.ExecuteScalar();
                if (existingUser > 0)
                {
                    ViewData["UsernameError"] = "Tên đăng nhập đã tồn tại!";
                    //ViewData["EnteredUsername"] = username;
                    return View();
                }
            }

            // Thêm user mới vào database
            string insertQuery = "INSERT INTO dbo.dangnhap (tentaikhoan, matkhau) VALUES (@tentaikhoan, @matkhau)";
            using (SqlCommand insertCmd = new SqlCommand(insertQuery, connection))
            {
                insertCmd.Parameters.AddWithValue("@tentaikhoan", username);
                insertCmd.Parameters.AddWithValue("@matkhau", password);
                insertCmd.ExecuteNonQuery();
            }
        }

        return RedirectToAction("Login");
    }

   
    ////////////////////////////////////////////////////////xử lý hàm mainchinh
    public IActionResult history() => View(); // Tra cứu lịch sử
    public IActionResult about() => View();   // Giới thiệu

    [HttpPost]
    public IActionResult ControlMotor(string command, string motor)
    {
        // Xử lý Start/Stop và chọn motor (robot/pump/fan)
        // Có thể redirect lại trang chính hoặc cập nhật thông tin điều khiển
        return RedirectToAction("mainprogram");
    }
    public IActionResult logout()
    {

        // Chuyển hướng về trang đăng nhập
        return RedirectToAction("Login");
    }
    public IActionResult mainprogram()
    {
        ViewData["Title"] = "Trang chủ";
        return View();
    }
    public IActionResult LoadPump()
    {
        return PartialView("_Pump");
    }
    //////////////////////////////////////////////////////////////xử lý đưa dữ liệu lên frontend và sqlSever
    [HttpGet]
    public async Task<IActionResult> GenerateSensorData()
    {
        string tempStatus, vibStatus, status;
        int nhietdo, dorung;
        var random = new Random();
        double temperature = Math.Round(random.NextDouble() * 100, 2); // Ví dụ 0–100 °C
        double vibration = Math.Round(random.NextDouble() * 10, 2);    // Ví dụ 0–10 G
        if (temperature > 70)
        {
            tempStatus = "Nguy hiểm";
            nhietdo = 3;
        }
        else if (temperature > 50)
        {
            tempStatus = "Cảnh báo";
            nhietdo = 2;
        }
        else
        {
            tempStatus = "Bình thường";
            nhietdo = 1;
        }
        // Đánh giá độ rung
        if (vibration > 7)
        {
            vibStatus = "Nguy hiểm";
            dorung = 3;
        }
        else if (vibration > 5)
        {
            vibStatus = "Cảnh báo";
            dorung = 2;
        }
        else
        {
            vibStatus = "Bình thường";
            dorung = 1;
        }
        if (nhietdo == 1 && dorung == 1)
        {
            status = "Hoạt Động Tốt";
        }
        else if (nhietdo == 3 && dorung == 3)
        {
            status = "Cần Kiểm Tra Gấp!!!";
        }
        else
        {
            status = "Cần Kiểm Tra";
        }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "INSERT INTO SensorData1 (DeviceID, TimeStamp, Temperature, Vibration, Status) VALUES (@DeviceID, @TimeStamp, @Temperature, @Vibration, @Status)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@DeviceID", 1);
                cmd.Parameters.AddWithValue("@TimeStamp", DateTime.Now);
                cmd.Parameters.AddWithValue("@Temperature", temperature);
                cmd.Parameters.AddWithValue("@Vibration", vibration);
                cmd.Parameters.AddWithValue("@Status", status);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }

        return Json(new { temperature, vibration, tempStatus, vibStatus });
    }





}

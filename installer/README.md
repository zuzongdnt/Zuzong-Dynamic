# Bộ cài Zuzong Dynamic

File phát hành: `outputs/ZuzongDynamic-Setup-1.0.0-x64.exe`.

Người dùng chỉ cần nhận một file này, mở lên rồi làm theo trình cài đặt. Không cần giải nén ZIP hoặc cài .NET riêng. App được cài cho tài khoản Windows hiện tại, mặc định ở `%LOCALAPPDATA%\Programs\ZuzongDynamic`; dữ liệu riêng ở `%LOCALAPPDATA%\ZuzongDynamic`.

- Windows 10 bản 2004 trở lên / Windows 11, x64.
- Có lựa chọn shortcut Desktop và tự chạy khi đăng nhập.
- Có mục gỡ trong Settings → Apps. Khi gỡ, các cấu hình ở hồ sơ riêng được giữ lại.
- Bộ cài không tự đóng app đang chạy. Bản cài đặt có mutex để yêu cầu thoát app trước khi cập nhật hoặc gỡ.
- Không kèm API key, phiên đăng nhập, lịch sử clipboard hay cấu hình notify của Zuzong. Mỗi người tự kết nối API/Spotify nếu sử dụng.
- Tính năng trực tuyến cần Internet. Phiên đăng nhập Spotify nhúng cho Canvas/lời bài hát còn cần WebView2 Runtime; luồng Spotify qua trình duyệt dùng trình duyệt mặc định.

`ZuzongDynamic.iss` dùng Inno Setup 7. Compiler bản dựng này là Tools.InnoSetup 7.1.0; đã kiểm tra Authenticode hợp lệ trước khi chạy. Inno Setup: https://jrsoftware.org/.

Để dựng lại, dùng .NET SDK và chạy `installer/build.ps1 -Compiler "đường dẫn ISCC.exe"`. Script tạo payload .NET self-contained rồi nén thành Setup.exe. Native DLL và shader có sẵn trong nguồn; không cần dựng lại native khi dùng các cờ trong script.

`InstallerQa.cs` là bộ thử riêng. Nó dùng một desktop riêng, không chuyển desktop của người dùng, và AppId QA tách biệt. Đã kiểm tra cài/gỡ, đăng ký Apps & Features, lựa chọn startup tắt, chạy app với CLR nằm trong thư mục cài đặt, chặn gỡ khi app còn chạy, và giữ nguyên hồ sơ sau khi gỡ.

Bản dựng chưa có chữ ký số của nhà phát hành Zuzong. SHA-256 nằm cạnh file cài đặt. Chưa thử trên máy Windows vật lý thứ hai; kiểm tra runtime đã xác nhận tiến trình dùng CLR kèm trong gói thay vì CLR cài trên máy thử.

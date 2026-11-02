# Zuzong Dynamic

Ứng dụng Dynamic Notch độc lập cho Windows, có trình phát nhạc, đồng hồ, khay file và giao diện kính lỏng tùy chỉnh.

- Tải bộ cài: https://github.com/zuzongdnt/Zuzong-Dynamic/releases/latest
- Báo lỗi / đề xuất: https://github.com/zuzongdnt/Zuzong-Dynamic/issues
- Tác giả bản tùy chỉnh: [Zuzong](https://github.com/zuzongdnt), TikTok [@_thinhdzuong_](https://www.tiktok.com/@_thinhdzuong_).

## Cài đặt và cập nhật

Windows 10 2004 trở lên / Windows 11, x64. Tải `ZuzongDynamic-Setup-<phiên bản>-x64.exe` trong Releases. Bộ cài kèm .NET, không cần cài runtime riêng.

Trong app, mở **Cài đặt nâng cao → Cập nhật → Kiểm tra**. Khi có bản mới, chọn **Tải và cài đặt**. App kiểm tra SHA-256 từ GitHub trước khi thoát, cài bản mới rồi mở lại. Cấu hình, API và phiên đăng nhập của người dùng được giữ nguyên. Có công tắc kiểm tra nền và nhận bản beta; không hạ cấp tự động. Chế độ ngoại tuyến chặn kiểm tra và tải cập nhật.

Hồ sơ người dùng nằm tại `%LOCALAPPDATA%\ZuzongDynamic`, tách khỏi thư mục cài đặt. Kho mã không chứa API key, lịch sử clipboard, phiên Spotify hay dữ liệu cá nhân trên máy phát triển. Mã QR và liên kết ủng hộ là thông tin tác giả chủ động công khai.

## Dựng bộ cài

Yêu cầu .NET SDK 8+ và Inno Setup 7. Native DLL và shader dựng sẵn được giữ trong nguồn; mã C++ và HLSL tương ứng cũng có sẵn.

```powershell
./installer/build.ps1 -Compiler 'C:\path\to\ISCC.exe'
dotnet run --project tests/ZuzongUpdateQA -c Release -p:SkipNativeCompile=true -p:SkipShaderCompile=true -- --isolated-data --settings E:/temp/zuzong-update-qa/notch.json
```

## Phát hành bản tiếp theo

1. Sửa `<Version>`, `<AssemblyVersion>` và `<FileVersion>` trong `src/ZuzongNotch/V-Notch.csproj`.
2. Commit mã nguồn, tạo tag `v<Version>` rồi push tag. Ví dụ `v1.0.3`.
3. Workflow `Release Zuzong Dynamic` tự dựng bộ cài, tạo SHA-256 và đăng GitHub Release. Tag có dấu `-` được phát hành dạng beta.
4. Chờ workflow thành công; kiểm tra bộ cài trong Releases. App đã cài sẽ tìm thấy bản này qua GitHub Releases API.

Không đưa khóa riêng, file `.env`, dữ liệu người dùng vào Git. GitHub Actions chỉ dùng quyền `contents: write` tích hợp, không cần chép token vào app.

## Ghi nhận và giấy phép

Bản tùy chỉnh dựa trên [V-Notch của w0otdev](https://github.com/w0otdev/V-Notch), giấy phép Apache-2.0. Giữ nguyên [giấy phép gốc](src/ZuzongNotch/LICENSE) và [ghi nhận thư viện](src/ZuzongNotch/docs/THIRD_PARTY_NOTICES.md). Thay đổi của Zuzong: ứng dụng độc lập, nhận diện Z, cấu hình riêng, tối ưu khởi động/kính lỏng, UI và cập nhật theo repo này.

Bộ cài hiện chưa có chứng thư ký mã của Zuzong. Hiệu ứng kính có thể không xuất hiện trong ảnh chụp/ghi màn hình do cơ chế loại Notch khỏi nguồn lấy nền.

# Bộ cài Zuzong Dynamic

Tải bộ cài tại https://github.com/zuzongdnt/Zuzong-Dynamic/releases . Tệp có dạng `ZuzongDynamic-Setup-<Version>-x64.exe`, đã kèm .NET Desktop Runtime.

Chạy `installer/build.ps1 -Compiler "C:\path\ISCC.exe"` trên Windows với .NET SDK 8+ và Inno Setup 7. Script đọc phiên bản từ csproj, xuất bộ cài và SHA-256 trong `outputs`. App cài tại `versions/<Version>`, hồ sơ riêng ở `%LOCALAPPDATA%\ZuzongDynamic`.

Workflow `.github/workflows/release.yml` kiểm tra tag, thử updater, dựng bộ cài và đăng GitHub Release. Tag `v<Version>` phải khớp csproj. Bản beta dùng `<Version>1.0.5-beta.1</Version>`, còn AssemblyVersion/FileVersion giữ dạng số `1.0.5.0`.

Không đưa API key, cấu hình notify theo máy, phiên Spotify hoặc lịch sử clipboard vào gói. Bộ cài giữ hồ sơ và lựa chọn khởi động cùng Windows khi nâng cấp. Nếu app còn giữ khóa, cần thoát trước. Updater đối chiếu SHA-256 do GitHub tính, chờ app thoát, cài rồi mở lại.

`InstallerQa.cs` dùng desktop và AppId QA riêng. Truyền workspace và phiên bản, ví dụ `InstallerQa.exe E:\appzuzong 1.0.3`. Bộ cài QA cần nằm tại `work/installer-test-build103/ZuzongDynamic-Setup-QA.exe`.

Bộ cài chưa có chứng thư ký mã của Zuzong. Chưa thử reboot thật hoặc máy vật lý thứ hai.

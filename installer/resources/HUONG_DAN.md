# Zuzong Dynamic — app Notch độc lập

Mở **ZuzongDynamic.exe**, hoặc shortcut **Zuzong Dynamic** trên Desktop/Start Menu. Khi app đang chạy, mở lại shortcut sẽ đưa bảng tùy chỉnh ra trước. App không cần cài hay chạy Windhawk, V-Notch gốc hoặc Taskbar Glass.

## Giao diện và tính năng

- Bảng chính: kiểu sát mép/viên thuốc nổi, chiều rộng thu/mở, khoảng cách mép trên, kính lỏng, màu, blur, khúc xạ, ánh sáng viền, logo, rê chuột và chạy cùng Windows. Bấm **Lưu giao diện**.
- **Tính năng**: bật/tắt các mô-đun riêng.
- **Cài đặt nâng cao**: nhạc, Spotify, Spotlight/chat AI, tiện ích mở rộng, hiệu ứng và các chức năng gốc đã ghép vào app.
- Khi tiện ích mở rộng là **Không**, trình phát tận dụng toàn bộ chiều ngang. Khi lời bài hát/phụ đề mở, phần lời có cột riêng; đóng lời thì media lấy lại khoảng trống.
- Nếu bật **Ẩn khi toàn màn hình**, Notch sẽ ẩn trong lúc video hoặc ứng dụng ở chế độ toàn màn hình. Đây là tùy chỉnh người dùng, không phải app đã tắt.
- Trang **Ủng hộ** dùng thông tin Dương Nhứt Thịnh / Zuzong, GitHub `zuzongdnt`, TikTok `_thinhdzuong_` và đúng ảnh QR được cung cấp. QR đã được giải mã lại từ ảnh giao diện đã render.

## Chat AI

Mở **Cài đặt nâng cao → Spotlight**. Chọn nhà cung cấp, API key và model; bấm **Kiểm tra kết nối**, sau đó **Lưu**. Nhấn **Alt + Space**, rồi **Tab** để chuyển sang chat.

Gemini chấp nhận cả `models/tên-model` và `tên-model`. Nút kiểm tra lấy danh sách model và thử nhận một câu trả lời. Bản này đã thử trực tiếp nhận trả lời thường và streaming với API key đang lưu. Hồ sơ chuyển sang bản độc lập giữ key được mã hóa; model Gemini 1.5 cũ được đổi sang `gemini-flash-lite-latest`, đã thử thành công tại thời điểm dựng bản.

## Spotify qua Google/Facebook

Đã làm sẵn đăng nhập qua trình duyệt mặc định bằng Authorization Code + PKCE. Không nhúng mật khẩu hay Client Secret vào app.

1. Mở **Spotify Developer Dashboard** từ cửa sổ kết nối, tạo ứng dụng Spotify cho tài khoản của bạn.
2. Thêm đúng Redirect URI: `http://127.0.0.1:43821/callback`.
3. Sao chép **Client ID** vào app, bấm **Đăng nhập bằng trình duyệt** rồi cấp quyền.

Chưa đăng nhập thật vì Zuzong chưa có Client ID. Đã kiểm tra tạo mã PKCE, địa chỉ redirect, phân tích callback và màn hình cấu hình. Nhạc/điều khiển media qua Windows vẫn dùng được khi chưa đăng nhập Spotify.

Spotify Web API không cung cấp Canvas/lời bài hát trong luồng này. Hai tính năng đó vẫn có mục **Phiên web cho Canvas / lời bài hát**, dùng đăng nhập email Spotify và quyền truy cập tương ứng; không khẳng định đăng nhập API sẽ mở khóa hai tính năng này.

## Dữ liệu, tự chạy và thông báo Codex

Cấu hình riêng ở `%LOCALAPPDATA%\ZuzongDynamic`. Không dùng hồ sơ taskbar để hoạt động. Trên máy Zuzong, hồ sơ Notch cũ đã được sao lưu rồi chuyển sang đây; chỉ Notch cũ được tắt, các cài đặt taskbar giữ nguyên.

**Chạy khi mở máy** lưu mục `ZuzongDynamic` trong HKCU Run, trỏ tới app này với `--startup`. Đã kiểm tra mục đăng ký và tiến trình chạy; chưa thử reboot/đăng nhập Windows thật trong lượt này.

Máy hiện tại giữ kết nối thông báo Codex qua `Bridge/ZuzongNotify.exe`, chuyển sự kiện vào hồ sơ độc lập và giữ nguyên chuỗi lệnh notify cũ. Gói ZIP không chứa cấu hình notify hay API key của người dùng. Nếu cài ở máy khác, chạy `python Bridge/install-codex-notify.py --app "đường dẫn thư mục app" --config "đường dẫn config.toml"` khi muốn ghép thông báo.

## Kiểm tra và mã nguồn

`QA` có kết quả kiểm tra và ảnh render từ chính cây giao diện WPF trên desktop thử riêng, không chụp desktop người dùng. Bố cục media đã thử ở rộng 320/480, sát mép/viên thuốc, không tiện ích và CPU/RAM. Kiểm tra các nút phát dùng dịch vụ nhạc giả để không tác động video đang xem. Lần thử video thật có ảnh 558×558 sau khoảng 2,3 giây; độ trễ còn phụ thuộc dữ liệu Windows và mạng.

Mã nguồn và bộ cài: https://github.com/zuzongdnt/Zuzong-Dynamic . Hướng dẫn dựng lại nằm trong README của repo. Bộ cài Setup.exe đã kèm .NET 8 Desktop Runtime x64; không cần cài runtime riêng.

Mở **Cài đặt nâng cao → Cập nhật → Kiểm tra** để tìm bản mới. Có thể bật kiểm tra nền và nhận bản beta. Nếu các nút bị khóa, xem thông báo chế độ ngoại tuyến; chỉ tắt chế độ này trong Quyền riêng tư khi muốn dùng mạng. App đối chiếu SHA-256 trước khi cài, giữ hồ sơ riêng và mở lại sau nâng cấp.

Zuzong Dynamic là bản sửa từ V-Notch 2.2.2 của w0otdev, theo Apache 2.0. Giữ bản quyền, LICENSE, THIRD_PARTY_NOTICES và nguồn gốc của các thành phần. Không nhận là mã gốc hoàn toàn của Zuzong.

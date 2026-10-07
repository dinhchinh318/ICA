# Luma Reef — database và sảnh 4 người

Yêu cầu Node.js 22.15 trở lên. Không cần npm install hoặc dịch vụ database bên ngoài.

```powershell
cd C:\Z_DDisk\ICA\ICA
node Server/server.mjs
```

Mặc định API chạy tại `http://127.0.0.1:8787`, database tại `Server/data/reef.sqlite`. Giữ cả file SQLite và các file `-wal`/`-shm` khi server đang chạy; nên dừng server trước khi sao lưu. Có thể đổi `REEF_HOST`, `REEF_PORT`, `REEF_DB` bằng biến môi trường. Client đọc địa chỉ từ `Assets/Resources/ServerConfig.json`; cần build lại sau khi đổi.

Trong sảnh game chọn **TÀI KHOẢN**, nhập tên và mật khẩu rồi **ĐĂNG KÝ** hoặc **ĐĂNG NHẬP**. Tài khoản mới nhận tiến trình khách và tối thiểu 100 triệu vàng, 9.999 kim cương. Hồ sơ khách hiện có được cộng gói quà một lần, đánh dấu `vipGrantVersion=1`. Sau đó tiền giảm bình thường khi sử dụng; đăng nhập không tự nạp lại tiền.

Tên tài khoản 3–24 ký tự ASCII chữ/số/gạch dưới; mật khẩu 8–128 ký tự. Mật khẩu được băm scrypt với salt riêng. Token ngẫu nhiên hết hạn sau 24 giờ, chỉ giữ trong RAM client; database chỉ lưu hash token. Mở game lại cần đăng nhập lại. Server dùng câu lệnh SQL có tham số, giới hạn thử đăng nhập, xác thực tài khoản cho từng lần lưu và kiểm tra revision để tránh ghi đè tiến trình từ thiết bị khác.

Game vào sảnh ngay cả khi server tắt. Hồ sơ khách và cache tài khoản tách biệt. Mỗi tài khoản có `accounts/<userId>/luma-reef-save.json` cùng `pending-profile.json` tại `Application.persistentDataPath`. Nếu thoát/mất mạng khi chưa gửi kịp, lần đăng nhập sau gửi lại bản chờ khi revision server vẫn khớp. Nếu server đã có bản khác mới hơn, tải bản server và giữ bản chờ để phục hồi thủ công; không tự ghi đè dữ liệu mới hơn.

**PHÒNG CHƠI** cho phép tạo phòng, lấy mã mời 8 ký tự, tham gia/rời phòng và xem bốn ghế tương ứng bốn góc. Ghế được cấp trong transaction; người thứ năm bị từ chối. Client gửi heartbeat mỗi 25 giây; ghế không hoạt động quá 120 giây được thu hồi khi API phòng chạy. Đây là **sảnh chờ**, chưa có đồng bộ trận đấu.

API:

| Route | Chức năng |
|---|---|
| GET `/health` | Kiểm tra server |
| POST `/v1/register`, `/v1/login` | `{username,password}`; đăng ký nhận thêm `profile` tùy chọn |
| GET, PUT `/v1/profile` | Đọc / lưu `{profile,revision}` của chủ token |
| POST `/v1/logout` | Hủy token và rời phòng |
| GET, POST `/v1/rooms` | Danh sách / tạo phòng |
| GET `/v1/rooms/<code>` | Trạng thái bốn ghế |
| POST `/v1/rooms/<code>/join`, `/leave`, `/heartbeat` | Quản lý người chơi trong phòng |

Các route sau đăng nhập dùng `Authorization: Bearer <token>`.

```powershell
node --test Server/test.mjs
```

Kiểm thử dùng SQLite tạm riêng: đăng ký, mật khẩu sai, trùng tên, phân tách hồ sơ, dữ liệu sai, revision cũ, tranh ghế đồng thời, phòng đủ bốn người, logout và khôi phục sau restart.

Đây là backend phát triển cho game tiền ảo. Lưu profile còn tin số tiền do client gửi để hỗ trợ prototype offline. Trước khi mở online công khai cần HTTPS và chuyển ví, phát bắn, RNG bắt cá, thưởng, cooldown sang server authority; thêm luồng snapshot/event trận đấu và reconnect. Không dùng API lưu profile hiện tại làm kinh tế cho phòng thi đấu.

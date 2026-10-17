# Mochi Reading — Flutter sinh viên

Flutter/Dart hiện thực 9 màn hình sinh viên từ [Google Stitch](https://stitch.withgoogle.com/projects/10519481668097040060?pli=1). Danh sách đề, bắt đầu/khôi phục, chọn/đổi/hủy, đồng hồ, nộp, kết quả và giải thích đều gọi backend ABP/SQL thật; không có mock fallback trong ứng dụng.

```powershell
flutter pub get
flutter run -d chrome --web-hostname localhost --web-port 5300 --dart-define=API_BASE_URL=http://localhost:5100
```

Backend phải đang chạy ở port 5100 với profile ToeicDevelopment. Ứng dụng vào thẳng thư viện, không có đăng nhập/đăng ký/đăng xuất. Backend tự mở phiên người học trong Default; cùng trình duyệt giữ danh tính và lịch sử bằng khóa ngẫu nhiên riêng. Chỉ đề PUBLISHED xuất hiện.

`lib/screens` chứa library/exam/result/review; `models` giữ contract kiểu dữ liệu; `services/reading_api.dart` mở phiên tự động, xử lý JWT, envelope, timeout 60 giây và tự gia hạn khi hết phiên; `session_store.dart` lưu khóa trình duyệt/request/attempt để refresh khôi phục. `widgets/mochi_theme.dart` chứa token/component từ thiết kế. Client ABP Session cũ được giữ cho kiểm thử tương thích.

Đồng hồ dùng deadline UTC của server và Stopwatch; đồng bộ mỗi 15 giây. Cảnh báo khi còn 5 phút là ngưỡng triển khai. Save gửi rowversion và option ID ổn định; kết quả do backend chấm. Giải thích bị khóa trước khi kết thúc. Bài đọc là 27 ảnh Word nguyên gốc, có thể phóng to; desktop Part 6/7 có hai cột.

Shared preferences phục vụ khôi phục phiên local; không lưu mật khẩu hoặc SQL connection string. Fonts Be Vietnam Pro/Inter và giấy phép OFL được bundle trong assets/fonts. Mascot lấy từ đúng HTML Stitch S09.

```powershell
flutter analyze
flutter test
flutter build web --release
```

Đã kiểm tra Chrome ở các kích thước đại diện trong quá trình phát triển; chưa nghiệm thu đầy đủ trên thiết bị Android thật. Android debug cho phép HTTP local, release cần URL HTTPS phù hợp. Hướng dẫn khôi phục cấu hình và chạy server có trong [backend/README](../backend/README.md).

Repository source chỉ gồm backend/frontend theo phạm vi upload, không chứa docs/Word/PDF/database dump/script gốc hoặc secrets. Các package và asset cần thiết được khôi phục từ pubspec.yaml/pubspec.lock, fonts và ảnh trong assets; không upload build/, .dart_tool/, Android local.properties hoặc keystore.

Để chạy Android emulator, cấu hình API của máy host khi build/run:

```powershell
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5100
```

Điện thoại thật cần địa chỉ backend truy cập được từ thiết bị và cấu hình HTTPS phù hợp; localhost của thiết bị không phải localhost máy tính. Client không cần và không chứa SQL connection string, mật khẩu SQL hoặc khóa ký JWT của backend.

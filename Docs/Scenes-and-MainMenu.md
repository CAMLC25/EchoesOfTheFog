# Scene và màn hình bắt đầu

## Cấu trúc scene

```text
Assets/Game/Scenes/
  Main/
    MainMenu.unity
    Level01_Forest.unity
    Level02_Village.unity
  Tests/
    AssetValidation.unity
    PlayerTest.unity
    CombatTest.unity
    RunnerTest.unity
```

`Main` chứa scene dùng trong luồng chơi. `Tests` chứa các scene kiểm tra từng phần, không đưa vào danh sách build chơi chính.

`Level01_Forest` là bản rừng có năm Runner, trước đây có tên `RunnerForestTest`. Màn hiện có nhiệm vụ mở đầu tìm/nhặt súng rồi tới cổng làng; xem [Forest-Opening.md](Forest-Opening.md). Nút **Vào làng** sau hoàn thành tải `Level02_Village`: map làng với máy phát/cầu hoạt động và đường tới trạm y tế; xem [Level02-Village.md](Level02-Village.md). Giải cứu NPC, Tanker và màn 3/Victory toàn game chưa hoàn chỉnh.

AssetValidation kiểm tra environment và Player; PlayerTest kiểm tra physics/camera; CombatTest kiểm tra damage với capsule; RunnerTest kiểm tra enemy và vật cản trong arena greybox.

Việc chuyển/đổi tên scene dùng `AssetDatabase.MoveAsset`, giữ nguyên GUID và file meta. Các đường dẫn trong Editor tools, PlayMode tests và tài liệu đã được cập nhật. Bản khôi phục trước khi chuyển nằm trong `.utmp/Backups/BeforeSceneOrganization`.

## Chạy menu

Mở `Assets/Game/Scenes/Main/MainMenu.unity`, nhấn Play.

- **Bắt đầu chơi** mở giới thiệu cốt truyện; **Vào Blackwood** hoặc **Bỏ qua** tải Level01_Forest. Elena bắt đầu tay không và cần nhấn F ở gần súng để trang bị. Năm Runner tuần tra trong rừng.
- **Hướng dẫn** mở bảng điều khiển và mẹo tránh thu hút Runner bằng tiếng súng/chạy. Nút **Trở về** hoặc Esc đóng bảng.
- Có thể chọn nút bằng phím lên/xuống và kích hoạt bằng Enter/Space. Chuột hiển thị và không bị khóa trong menu.
- Esc trong màn rừng mở Pause với Tiếp tục, Chơi lại và Về menu; khi chết hiện Game Over. Xem [Pause-and-GameOver.md](Pause-and-GameOver.md). Scene test độc lập vẫn giữ Esc thả chuột.

Build Settings có hai scene theo thứ tự MainMenu, Level01_Forest. Không ép Editor luôn bắt đầu Play từ menu, để vẫn có thể mở các scene test trực tiếp.

## Giao diện và tài nguyên

Background tham chiếu trực tiếp `Assets/Game/UI/Backgrounds/Background.png`, không sửa file ảnh. Ảnh giữ tỷ lệ và phủ khung hình, cắt bớt cạnh khi tỷ lệ màn hình khác nguồn. Lớp gradient UI tăng tương phản ở phần chữ bên trái. Nhân vật và ngôi làng trong background giữ vai trò hình ảnh chính.

Menu dùng Canvas/uGUI và TextMeshPro cùng Input System UI module. Cả 31 nhãn đã chuyển từ Text Legacy sang TextMeshProUGUI. Font Liberation Sans đi kèm TMP được đóng thành font SDF tĩnh có đủ ký tự tiếng Việt, tại `Assets/Game/UI/Fonts/MenuVietnameseSDF.asset` (SDFAA, atlas 2048×2048, sampling 90, padding 9). Tài nguyên Essential Resources được import từ package UI đã cài, không nâng/cài package mới; giấy phép font nằm ở `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`.

Canvas tham chiếu 1920×1080, bật Pixel Perfect và tự scale theo kích thước cửa sổ. Chữ nút tăng lên 30; chữ điều khiển trong Hướng dẫn 24–26; các dòng phụ cũng được tăng cỡ và tương phản. Ô phím được nới để chữ "CHUỘT PHẢI"/"CHUỘT TRÁI" không xuống dòng. Nút có trạng thái hover, nhấn, chọn bằng bàn phím và khóa khi bắt đầu tải scene để tránh tải lặp.

`MainMenuController` quản lý hai bảng và tải scene bất đồng bộ. `GameScenes` cung cấp đường dẫn MainMenu/Forest. `MenuGradient` chỉ tô màu vertex UI. `MainMenuSetup.Apply` là công cụ Editor batch tường minh để phân loại scene/tạo menu; không tự chạy khi mở project. Menu hoặc scene đã tồn tại sẽ được giữ để không ghi đè chỉnh sửa tay.

`MainMenuTypography.Apply` là migration typography riêng, giữ scene GUID, đối tượng, background và dây nối nút, thay component chữ và chỉnh thông số font/layout liên quan. Nó chạy tường minh khi Unity đã đóng. Backup scene trước migration ở `.utmp/Backups/BeforeMenuTypography`. Tài nguyên TMP/font đã được lưu trong Assets nên mở lại project không phải tạo glyph theo font hệ điều hành.

## Kiểm chứng

Kiểm thử chuột/bàn phím cho Hướng dẫn, Esc/Trở về, bắt đầu tải màn rừng, một Player/một AudioListener và điều khiển sau chuyển scene. Ảnh render menu, hướng dẫn và tỷ lệ 4:3 nằm trong `Logs/MainMenu`. Bộ PlayMode hiện có được chạy lại để kiểm tra các đường dẫn sau khi chuyển scene. Logs không nằm trong commit.

Kết quả: **32/32 PlayMode**, **23/23 EditMode** pass. Đã xem ảnh menu và hướng dẫn ở 1920×1080, menu ở 1280×960; các nút nằm trong viewport và chữ tiếng Việt hiển thị. Nội dung cả năm scene trước khi phân loại được đối chiếu bằng hash với bản backup; chỉ vị trí/tên asset thay đổi.

Sau khi đổi sang SDF: chạy lại **3/3 kiểm thử menu**, gồm chuột/bàn phím, đóng/mở Hướng dẫn và tải màn rừng với điều khiển hoạt động. Kiểm thử bổ sung phát hiện chữ tràn, nhãn nút/phím xuống dòng và glyph tiếng Việt bị thiếu. Đã xem ảnh mới ở 1920×1080, 1280×960 và 1360×552 (gần khung Game View người dùng gửi). Ảnh trước thay đổi giữ trong `Logs/MainMenu/BeforeTypography`; ảnh mới ghi tại các file Menu/Guide tương ứng.

Chưa build Windows, chưa thêm âm thanh menu, pause, chọn màn hoặc lưu tiến trình. Chưa commit/push thay đổi này.

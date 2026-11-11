# Echoes of the Fog: The Extraction

**Tiếng Vọng Trong Sương: Cuộc Giải Cứu** — đồ án game 3D Unity của **Nhóm 15**, học phần Lập trình Game.

Game hành động sinh tồn góc nhìn thứ ba, lấy bối cảnh ngôi làng Blackwood bị cô lập sau một đợt bùng phát bào tử nấm độc. Người chơi vào vai **Elena**, một chuyên viên quân y tiến vào vùng dịch để tìm mẫu dịch chiết bào tử, giải cứu em gái và những người sống sót, rồi tìm đường thoát bằng xe tải.

![Hình nền menu Echoes of the Fog](Assets/Game/UI/Backgrounds/Background.png)

> **Trạng thái:** prototype đang phát triển. Hiện có hai màn chơi thử nối tiếp nhau; chuỗi giải cứu và màn Extraction cuối game chưa hoàn thiện. Ảnh trên là hình nền menu, không phải ảnh gameplay.

## Tính năng hiện có

- Elena với animation di chuyển, chạy, nhảy, ngắm, bắn, nạp đạn và phản ứng trúng đòn; camera third-person có xử lý vật cản.
- Súng **Gun_31**, bắn hitscan, băng đạn giới hạn, đạn dự trữ và nạp đạn. Có thể bắn khi ngắm hoặc bắn thường.
- Máu, thể lực, nhặt súng/đạn/cứu thương và dùng cứu thương.
- **Runner AI bằng C# FSM + NavMeshAgent**: tuần tra, phát hiện bằng tầm nhìn/âm thanh, truy đuổi, tấn công, tìm kiếm khi mất dấu và chết. Không dùng ML-Agents.
- Main Menu, hướng dẫn, giới thiệu cốt truyện, HUD mục tiêu, Pause, Game Over, chơi lại và trở về menu.
- Môi trường Flooded Grounds, ánh sáng/fog, collider và NavMesh riêng cho từng màn.

## Hai màn chơi thử

| Màn | Nội dung hiện tại | Cách đi tiếp |
|---|---|---|
| **Level01_Forest** — Bìa rừng | Bắt đầu tay không, tìm súng, vượt tuyến rừng với 5 Runner và hai bàn tiếp tế. | Tới cổng làng khi đã có súng, chọn **Vào làng**. |
| **Level02_Village** — Làng bỏ hoang | 4 Runner, ba bàn tiếp tế; tìm nguồn điện, hạ cầu qua kênh rồi tới trạm y tế. | Hiện dừng ở bước tiếp cận trạm y tế; chưa có NPC giải cứu hay chuyển sang màn 3. |

Ở màn 2, rẽ trái tại phố chính để tìm sân máy phát có đèn vàng. Nhấn **F** tại công tắc để hạ cầu, sau đó quay lại đường chính và đi tới nhà có dấu thập đỏ. Rơi xuống kênh sâu sẽ gây Game Over.

Mỗi lần vào/chơi lại màn 2, Player bắt đầu với **100 máu, súng, băng 30 viên, 90 đạn dự trữ và 0 cứu thương**. Máu và vật phẩm từ màn 1 hiện chưa được chuyển sang màn 2.

## Công nghệ

| Thành phần | Cấu hình |
|---|---|
| Unity Editor | **6000.3.26f1 — Unity 6.3 LTS** |
| Nền tảng mục tiêu | PC Windows, single-player |
| Ngôn ngữ | C# |
| Render pipeline | Built-in Render Pipeline |
| Input System | `com.unity.inputsystem` **1.20.0** |
| Camera | Cinemachine **3.1.7** |
| Điều hướng AI | AI Navigation **2.0.16**, NavMeshAgent |
| Giao diện | uGUI, TextMeshPro với font SDF tiếng Việt |
| Quản lý asset lớn | Git LFS |

Phiên bản chính xác nằm trong [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt); dependencies nằm trong [manifest.json](Packages/manifest.json). Khi mở project, dùng đúng phiên bản Editor và để Unity khôi phục package đã khai báo.

## Tải và mở project

Cần có **Git**, **Git LFS**, **Unity Hub** và **Unity Editor 6000.3.26f1**. Cài thêm module build Windows phù hợp nếu muốn xuất bản chạy độc lập.

```bash
git lfs install
git clone https://github.com/CAMLC25/EchoesOfTheFog.git
cd EchoesOfTheFog
git lfs pull
```

1. Trong Unity Hub, chọn **Add project from disk** và trỏ tới thư mục `EchoesOfTheFog` chứa `Assets`, `Packages`, `ProjectSettings`.
2. Mở bằng Editor **6000.3.26f1**, chờ import asset và khôi phục package hoàn tất.
3. Mở `Assets/Game/Scenes/Main/MainMenu.unity`, nhấn **Play**.
4. Chọn **Bắt đầu chơi**, xem giới thiệu rồi chọn **Vào Blackwood** hoặc **Bỏ qua**.

Có thể mở trực tiếp `Level01_Forest.unity` hoặc `Level02_Village.unity` để kiểm tra từng màn. Nếu model/texture không tải đủ sau khi clone, kiểm tra `git lfs pull` trước khi chỉnh lại asset trong Unity.

## Điều khiển

| Phím / thao tác | Chức năng |
|---|---|
| **W A S D** | Di chuyển |
| **Chuột** | Xoay camera |
| **Giữ Shift** | Chạy, tiêu hao thể lực |
| **Space** | Nhảy |
| **Giữ chuột phải** | Ngắm khi đã có súng |
| **Chuột trái** | Bắn khi đã có súng |
| **R** | Nạp đạn từ kho dự trữ |
| **F** | Nhặt súng/vật phẩm hoặc tương tác công tắc gần nhất |
| **E** | Dùng một cứu thương |
| **Esc** | Tạm dừng / tiếp tục trong hai màn chính |
| **Phím mũi tên + Enter** | Chọn và kích hoạt nút trong menu |

Khi chuột chưa được khóa, click vào Game View để điều khiển. Ở các scene test độc lập, Esc vẫn thả chuột thay vì mở Pause.

- Mỗi hộp đạn cung cấp **30 viên**; kho dự trữ tối đa **180 viên**. Cần nhặt súng trước khi nhận đạn; đạn nhặt được không tự điền vào băng.
- Nếu kho chỉ còn chỗ cho một phần đạn, hộp giữ lại phần còn dư để nhặt sau.
- Mang tối đa **3 cứu thương**, mỗi túi hồi tối đa **40 máu**. Không tiêu hao khi máu đầy và không hồi sinh khi đã chết.
- Pause có **Tiếp tục / Chơi lại / Về menu**; Game Over có **Chơi lại / Về menu**. Chơi lại khôi phục màn hiện tại từ đầu, chưa có checkpoint giữa màn.

## Cấu trúc repository

```text
Assets/
  Flooded_Grounds/        # Thư viện môi trường gốc
  Game/
    Animations/          # Animation và Animator Controller
    Art/                 # Vật phẩm, mesh/material môi trường của game
    Characters/          # Model nhân vật
    Data/                # Cấu hình vũ khí, enemy và dữ liệu liên quan
    Editor/              # Công cụ dựng, kiểm tra và chụp scene
    Prefabs/             # Player, enemy, vũ khí, vật phẩm
    Scenes/
      Main/              # MainMenu, Level01_Forest, Level02_Village
      Tests/             # AssetValidation, PlayerTest, CombatTest, RunnerTest
    Scripts/             # Gameplay, AI, tương tác, nhiệm vụ và UI
    Tests/               # EditMode và PlayMode tests
    UI/                  # Hình nền, font và tài nguyên UI
  StreamingAssets/
    GameConfig.csv       # Thông số enemy
Packages/                # Dependencies Unity
ProjectSettings/         # Cấu hình project và danh sách scene build
Docs/                    # Ghi chú kỹ thuật và hướng dẫn từng hệ thống
```

Giữ các file `.meta` khi làm việc với asset để bảo toàn GUID và tham chiếu. Các thư mục `Library`, `Temp`, `Logs`, `UserSettings` và `.utmp` được Git bỏ qua. Công cụ dựng scene trong `Editor` chỉ phục vụ authoring; không cần chạy chúng để chơi các scene đã lưu.

## Kiểm thử và build

Trong Unity, mở **Window → General → Test Runner** để chạy **EditMode** hoặc **PlayMode**. Lượt PlayMode gần nhất trước khi viết README đạt **51/51 bài kiểm thử**, bao gồm di chuyển/nhảy, combat, Runner, vật phẩm, menu, Pause/Game Over, nhiệm vụ mở đầu và chuyển sang màn làng.

Các bài kiểm thử màn làng kiểm tra F bật nguồn, cầu đóng/mở và NavMesh, Player đi qua collider cầu thật, rơi xuống kênh và chơi lại đúng màn. Kiểm thử tự động chưa thay thế một lượt chơi thủ công để đánh giá camera, nhịp giao tranh và chất lượng hình ảnh.

Để build Windows, mở **Build Profiles**, chọn nền tảng Windows và giữ ba scene được bật theo thứ tự:

1. `Assets/Game/Scenes/Main/MainMenu.unity`
2. `Assets/Game/Scenes/Main/Level01_Forest.unity`
3. `Assets/Game/Scenes/Main/Level02_Village.unity`

Xuất vào thư mục `Builds/` rồi kiểm tra từ menu tới hai màn trên bản chạy độc lập. Các scene trong `Scenes/Tests` không thuộc luồng build chính. Kết quả PlayMode ở trên không phải xác nhận rằng bản build Windows đã được smoke test trên máy khác.

## Phần đang phát triển

- Tanker: đã có model trong project, chưa tích hợp animation và AI vào màn làng.
- NPC sống sót, hội thoại, giải cứu, nhận chìa khóa và cơ chế đi theo.
- Mẫu dịch chiết bào tử và chuỗi nhiệm vụ cốt truyện đầy đủ.
- **Level03_Extraction**: xe tải, bảo vệ quá trình khởi động khoảng 45 giây và Victory.
- Chuyển trạng thái Player giữa các màn, lưu tiến trình/cài đặt, âm thanh và cân bằng gameplay.
- Thay model máy phát tạm; hoàn thiện nội thất trạm y tế, trang trí, ánh sáng và hiệu năng.

## Tài liệu chi tiết

- [Scene, Main Menu và giao diện](Docs/Scenes-and-MainMenu.md)
- [Giới thiệu cốt truyện, nhặt súng và nhiệm vụ mở đầu](Docs/Forest-Opening.md)
- [Đạn và cứu thương](Docs/Ammo-and-Medkits.md)
- [Pause và Game Over](Docs/Pause-and-GameOver.md)
- [Thiết kế màn 2 — Village](Docs/Level02-Village.md)
- [Runner AI](Docs/Runner-AI.md)
- [Player, animation và súng](Docs/Player-Animations-Rifle.md)

## Tài nguyên và ghi nhận

- **Flooded Grounds**: môi trường, công trình, cây cối và đạo cụ.
- **Adobe Mixamo**: nhân vật và animation theo tài nguyên đã import vào project.
- **Free Low Poly Weapons**: model súng Gun_31.
- **TextMeshPro / Liberation Sans**: hiển thị chữ tiếng Việt; giấy phép font tại [LiberationSans - OFL.txt](Assets/TextMesh%20Pro/Fonts/LiberationSans%20-%20OFL.txt).

Các asset bên thứ ba giữ giấy phép riêng theo gói tài nguyên đi kèm. Repository hiện chưa khai báo giấy phép riêng cho mã nguồn.

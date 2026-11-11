# AssetValidation — Forest to Blackwood visual study

Scene: `Assets/Game/Scenes/AssetValidation.unity`.

## Phạm vi

Thay bãi thử primitive Phase 1 bằng một đoạn môi trường từ bìa rừng đến cổng làng, khoảng 70 m. Đây là scene duyệt hình ảnh và layout, chưa phải level gameplay hoàn chỉnh. Player capsule và camera third-person đã được tích hợp trong [Phase 2](Phase2-Player.md); chưa có combat, objective hoặc enemy.

Scene trước khi dựng lại, gồm cả cây người dùng vừa sửa/thử, được giữ ở `Assets/Game/Scenes/Archive/AssetValidation_BeforeForestStudy.unity`. Không ghi đè bản dự phòng khi chạy lại công cụ.

## Bố cục và tài nguyên

- Đường đất uốn nhẹ, nền rừng và bờ đất có cao độ; không còn tấm nền vuông phẳng làm nội dung chính.
- Một rãnh nước ngang đường, lối ván từ `Struct_WoodPath_A`, collider sàn riêng vì prefab nguồn không có collider.
- Cây TreeCreator người dùng đã sửa được dùng làm các cụm rừng, cây bụi tự nhiên, cỏ và đá. Không dùng cây primitive làm môi trường cuối.
- Cổng đá, hàng rào và cabin từ Flooded Grounds, ánh đèn ấm đánh dấu đích giữa môi trường lạnh.
- Đo kích thước mesh trước khi đặt: nhiều cây nguồn cao 37–47 m; cabin gần 10 m, cổng 7 m. Tree scale theo chiều cao mục tiêu, cabin/cổng dùng scale thống nhất 0.62. Đây là tỷ lệ visual study, cần đối chiếu lại khi có Player thật.
- Material/shader mới và các bản sao material chỉnh màu nằm trong `Assets/Game/Art/ForestEntryStudy`; không sửa material gốc để đổi màu cả asset pack.
- Nền dùng mesh có collider và shader Built-in trộn texture Dirt/Moss nguồn theo mép đường. Nước là mặt nước minh họa; chưa có bơi, dòng chảy hoặc hệ thống tương tác.

## Xem trong Unity

1. Mở AssetValidation. Game View hiện dùng `Main Camera - Player`; nhấn Play và click Game View để điều khiển capsule.
2. Trong Hierarchy, mở `Forest_To_Blackwood_VisualStudy/07_Preview_Cameras_And_Markers`.
3. Chọn `02_TimberCrossing` hoặc `03_VillageReveal` để xem Camera Preview trong Scene View. Các camera này được tắt mặc định để chỉ Main Camera render.
4. `04_LayoutOverview` là góc tổng quan. Ảnh batch của góc này tạm tắt fog để đọc layout; fog trong scene vẫn giữ nguyên.
5. Chọn root môi trường và nhấn F nếu Scene View đang giữ góc nhìn cũ từ bãi thử.

Các camera `01`–`04` là preview cố định và hiện tắt để không tranh quyền với camera Player. Xem hướng dẫn điều khiển và giới hạn camera trong tài liệu Phase 2.

## Kiểm chứng

- Công cụ authoring: `Assets/Game/Editor/ForestEntryStudy.cs`, chỉ thực thi khi gọi tường minh trong Editor batch mode. Không chạy tự động lúc import hoặc trong player build.
- NavMeshSurface bake bằng Physics Colliders; dữ liệu lưu trong thư mục art của visual study.
- Kiểm tra mesh/material của scene, đường NavMesh hoàn chỉnh từ PlayerStart đến phía trong cổng và raycast mặt sàn tại 70 điểm theo tuyến.
- Báo cáo máy: `Logs/ForestEntryStudy/validation.txt`.
- Kết quả: Unity compile/batch build exit code 0; mesh/material không thiếu, 70/70 raycast có mặt sàn và NavMesh path tới làng hoàn chỉnh. Đã render và xem lại ba góc gần mặt đất sau tinh chỉnh.
- Ảnh render: `01_ForestEntry.png`, `02_TimberCrossing.png`, `03_VillageReveal.png`, `04_LayoutOverview.png` trong `Logs/ForestEntryStudy`.
- Chưa đo FPS/build Windows hoặc test Player qua vai. Không coi đường NavMesh hợp lệ là bằng chứng đầy đủ về trải nghiệm di chuyển.

## Khi chỉnh tiếp

Chỉnh các instance, ánh sáng hoặc mesh/material thuộc Game trong Editor. Giữ root theo nhóm để dễ review. `Build` sẽ tái tạo visual study từ layout trong code, nên không chạy lại sau khi đã sửa tay mà chưa sao lưu. `Capture` và `ValidateScene` không lưu sửa scene.

Các thay đổi TreeCreator, MSC_HumanScale và Scene_A có sẵn từ thao tác người dùng trước lượt dựng map này. Không tự revert các thay đổi đó. Chưa stage/commit.

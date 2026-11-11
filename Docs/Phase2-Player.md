# Phase 2 — Player capsule và camera

## Chạy thử

Mở `Assets/Game/Scenes/AssetValidation.unity`, nhấn Play, click vào Game View.

| Điều khiển | Chức năng |
|---|---|
| WASD / phím mũi tên | Di chuyển theo hướng nhìn camera |
| Rê chuột | Xoay camera |
| Giữ Left Shift | Chạy nếu còn stamina và không ngắm |
| Space (phím cách) | Nhảy khi chạm đất, độ cao mục tiêu 1.2 m; không nhảy đôi hoặc tự nhảy lặp khi giữ phím |
| Giữ chuột phải | Ngắm: thu FOV, đưa camera gần vai, đi chậm và xoay người theo camera |
| Esc | Nhả chuột và dừng nhận điều khiển Player; không phải Pause toàn game |
| Click chuột trái | Bắt lại chuột sau Esc/mất focus; chưa bắn |

Chưa có crouch, súng, animation, enemy hoặc mission. Các action mẫu còn trong Input Actions không đồng nghĩa tính năng đó đã được triển khai. F dành cho tương tác giai đoạn sau; E dành cho hồi máu sau này.

`Assets/Game/Scenes/PlayerTest.unity` là sân thử độc lập với sàn, tường, bậc 0.30 m và vật cản 1 m. Dùng để kiểm tra di chuyển/camera trước khi thử trong rừng.

## Cấu trúc bàn giao

- `Prefabs/Player/PlayerCapsule.prefab`: root tag/layer Player, Rigidbody, CapsuleCollider, Health, input, motor, stamina, camera rig và HUD prototype.
- `Scripts/Player/PlayerInputReader.cs`: clone Input Actions khi chạy để không bật/tắt asset dùng chung. Esc/mất focus nhả chuột. Disable/destroy dọn action instance.
- `PlayerMotor.cs`: vật lý trong FixedUpdate, tốc độ đi/chạy/ngắm, xoay theo hướng đi, nhận diện mặt sàn, bậc thấp và xử lý Health.Died. Nhận diện mặt sàn bỏ qua layer Player và trigger.
- `PlayerStamina.cs`: cạn về 0 thì ngừng chạy, hồi sau cooldown; phải hồi đạt ngưỡng trước khi được chạy lại. Chỉ tiêu hao khi thực sự di chuyển ở tốc độ chạy, không khi giữ Shift đứng yên hoặc đẩy vào tường.
- `PlayerCameraRig.cs`: yaw/pitch độc lập hướng quay thân, giới hạn pitch, chuột theo delta và stick theo thời gian; chuyển khoảng cách/FOV khi ngắm.
- `PlayerPrototypeHUD.cs`: hiển thị Health, Stamina và điều khiển để review prototype; chưa phải UI cuối.
- `Data/Player`: material capsule và PhysicsMaterial giảm ma sát để không dính tường.
- `Editor/Phase2Setup.cs`: setup batch tường minh, không chạy tự động. Không ghi đè prefab Player đã tồn tại hoặc layout map.

## Camera và physics

Cinemachine **3.1.7**: `CinemachineCamera` + `CinemachineThirdPersonFollow` thuộc prefab Player, xuất qua `Main Camera - Player` với `CinemachineBrain`. Camera preview cũ giữ trong scene nhưng bị disable; chỉ có một Camera và AudioListener bật.

Rigidbody dùng gravity, mass 70, Interpolate, Continuous Dynamic và khóa xoay vật lý; motor chủ động xoay yaw. Capsule cao 1.8 m, bán kính 0.3 m, origin ở chân. Không gắn CharacterController cùng Rigidbody.

Đã thêm layer **Player** vào một slot trống của TagManager để ground probe và camera không tự va vào capsule. Không thay ma trận collision toàn project, Input Handling, render pipeline hoặc package version.

Camera tránh các vật cản có collider bằng ThirdPersonFollow.AvoidObstacles. Tán lá không có collider riêng vẫn có thể che camera; đây là giới hạn cần review trong map thực tế. Bậc thấp chỉ thử khi có mặt sàn dưới chân và có khoảng trống cho capsule; không cho leo tường cao.

## Health và stamina

Health mặc định **100**, dùng lại API Phase 1. Khi hết HP: motor dừng điều khiển, input bị khóa, chuột được nhả; gravity vẫn hoạt động. Chưa có Game Over/restart flow; dừng Play rồi chạy lại để reset prototype. Rơi dưới Y=-12 gây chết.

Giá trị prototype trong Inspector: đi 2.5 m/s, chạy 5.2 m/s, ngắm 1.8 m/s; stamina 100, tiêu hao 22/s, hồi 18/s sau 1.2 s; phục hồi ít nhất 25% sau khi cạn. Các giá trị này chưa phải balance cuối.

## Bảo toàn scene

Scene trước khi thêm Player: `Assets/Game/Scenes/Archive/AssetValidation_BeforePlayer.unity`. Không dựng lại environment hoặc sửa asset Flooded Grounds trong Phase 2. Các sửa cây/material/Scene_A có sẵn từ người dùng được giữ.

Object NavMesh đang tắt trong scene khi bắt đầu Phase 2; Player Rigidbody không cần NavMesh. Test walkthrough chỉ bật object đó trong phiên Play Mode để kiểm tra đường đi, không lưu việc bật lại vào scene.

Không gọi lại ForestEntryStudy.Build sau khi đã tích hợp Player mà chưa sao lưu: công cụ đó tái tạo scene môi trường ban đầu. Phase2Setup.Run bỏ qua prefab/Player đã tồn tại để tránh ghi đè chỉnh sửa tay.

## Kiểm thử và review

- EditMode: Health/config Phase 1 và quy tắc stamina.
- PlayMode: input chéo không tăng tốc; sprint, hồi stamina, đứng yên, đẩy tường; bậc thấp; aim, death, Esc/click; camera sát tường; đi qua lối gỗ tới làng.
- PlayMode tests giả lập keyboard/mouse, không phải một người đã điều khiển bằng tay. Ảnh camera từ Play Mode lưu tại `Logs/Phase2`.
- Kết quả máy lưu ở `Logs/Phase2/editmode-results.xml`, `playmode-results.xml` và `setup-validation.txt`.
- Kết quả Phase 2: **23/23 EditMode** pass. Sau bổ sung Space, **13/13 PlayMode** pass (`Logs/Phase2/jump-playmode-results.xml`), gồm nhảy/tiếp đất, giữ Space không bật lặp, không nhảy đôi và không nhận nhảy lúc nhả chuột. Walkthrough điều khiển capsule thực tế qua mặt lối gỗ và tới phía trong làng; không chỉ kiểm tra NavMesh. Đã xem ảnh camera từ Play Mode tại điểm bắt đầu và tiếp cận làng.

Review thủ công: đi hết tuyến bằng WASD, chạy qua lối gỗ, xoay camera sát cây/hàng rào, ngắm khi đang di chuyển, cạn/hồi stamina, nhả/bắt chuột. Chưa đo FPS hoặc build Windows. Sau Phase 2 dừng chờ review trước Enemy FSM.

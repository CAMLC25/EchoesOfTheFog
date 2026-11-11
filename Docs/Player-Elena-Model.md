# Elena — gắn model vào Player

> Đây là ghi nhận bước gắn model ban đầu. Animation và súng đã được bổ sung sau đó; trạng thái hiện tại xem [Player-Animations-Rifle.md](Player-Animations-Rifle.md).

Model dùng sẵn: `Assets/Game/Characters/Player/Elena_Model.fbx` (skeleton Mixamo, 52 xương).

## Thay đổi

- Model importer chuyển sang Humanoid, Create From This Model; Avatar hợp lệ.
- Hai texture `Ch11_1001_Normal.png` và `Ch11_1002_Normal.png` được đánh dấu Normal Map; ảnh nguồn không thay đổi.
- Thêm `ElenaVisual/Elena_Model` trong `Assets/Game/Prefabs/Player/PlayerCapsule.prefab`; giữ liên kết FBX và hai material đã extract.
- Căn chiều cao visual 1.75 m và chân về origin bằng vertex sau skinning, không dùng bounding box đã đệm của renderer. Uniform scale khoảng 1.0025.
- `CapsuleVisual` chỉ tắt, không xóa; Rigidbody, CapsuleCollider 1.8 m, Health, stamina, input và Cinemachine giữ nguyên.
- Layer của model/bones theo layer Player; model không thêm collider hoặc Rigidbody riêng. Animator tắt Apply Root Motion để physics điều khiển vị trí.
- Bản prefab trước thay đổi: `Assets/Game/Prefabs/Player/Archive/Player_BeforeElena.prefab`.
- Công cụ editor: `Assets/Game/Editor/PlayerModelSetup.cs`; chỉ chạy khi gọi batch tường minh, không chạy tự động khi import.

## Animation

Theo lựa chọn của người dùng, bước này chỉ gắn model. FBX có một clip nhúng dài khoảng một frame (`mixamo.com`), không phải Idle/Walk/Run. Chưa gắn Animator Controller hoặc giả lập hoạt ảnh bằng code. Nhân vật giữ tư thế T-pose khi di chuyển/nhảy.

Bước sau cần import animation Mixamo Idle, Walk, Run, Jump (ưu tiên In Place và Without Skin), cấu hình Humanoid và retarget với Avatar Elena, rồi tạo Blend Tree/Animator Controller liên kết vận tốc, grounded và aim. Không bật root motion khi vẫn dùng PlayerMotor hiện tại.

## Xem và kiểm tra

Mở AssetValidation hoặc PlayerTest → Play → click Game View. Điều khiển WASD, Shift, chuột phải và Space như trước. Prefab được dùng chung nên hai scene nhận visual mới.

Ảnh kiểm chứng ở `Logs/PlayerModel/Elena_InMap.png` và `Elena_Front.png`; thông tin rig/scale ở `inspection.txt`, `placement.txt`. Không sửa environment hoặc model enemy trong bước này. Chưa commit/push.

Kết quả: setup/capture Unity exit code 0, Avatar valid/human = True/True, đã xem ảnh trước/sau nhân vật. Bộ kiểm thử PlayMode hiện có **13/13 pass**, kết quả ở `Logs/PlayerModel/playmode-results.xml`. Chưa có animation nên kết quả này xác nhận physics/input/camera vẫn hoạt động, không phải đã kiểm thử chuyển động đi/chạy của model.

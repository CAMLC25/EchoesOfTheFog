# Phase 1 — Foundation

> Cập nhật sau Phase 1: AssetValidation đã được dựng lại thành đoạn rừng–cổng làng theo yêu cầu người dùng. Bãi thử cũ được lưu trong Scenes/Archive. Xem [AssetValidation-ForestStudy.md](AssetValidation-ForestStudy.md) để biết nội dung scene hiện tại; các mô tả bãi thử bên dưới là hồ sơ Phase 1 ban đầu.

## Baseline kỹ thuật

- Unity **6000.3.26f1**, Windows, Built-in Render Pipeline.
- Active Input Handling: **Both**. Gameplay mới sẽ dùng Input System; demo Flooded Grounds vẫn dùng Input Manager.
- AI Navigation **2.0.16**, Cinemachine **3.1.7**. Splines là dependency của Cinemachine; không phải một hệ thống gameplay mới.
- Unity đã resolve thêm Splines **2.9.1** và Mathematics **1.3.3**; các dependency hiện có giữ nguyên phiên bản.
- Enemy chỉ dùng **C# FSM + NavMeshAgent**. Chưa triển khai FSM trong Phase 1.
- Asset gốc giữ ở `Assets/Flooded_Grounds`; nội dung game ở `Assets/Game`.
- Tên namespace bắt đầu bằng `EchoesOfTheFog`; mỗi component chịu một trách nhiệm. Tham số gameplay không rải hard-code trong AI.

## API bàn giao

### Damage và Health

`EchoesOfTheFog.Core.IDamageable.TakeDamage(float amount)` nhận số hữu hạn lớn hơn 0.

`Health` dùng chung cho Player, enemy, NPC:

- `CurrentHealth`, `MaxHealth`, `IsDead`: trạng thái chỉ đọc từ bên ngoài.
- `HealthChanged(current, maximum)`: cập nhật HUD; khi subscribe cần đọc trạng thái ban đầu ngay.
- `Died`: phát một lần mỗi vòng đời. Enemy/Mission tự subscribe; Health không tự destroy GameObject hoặc chuyển scene.
- `Heal(amount)`: giới hạn ở max HP; không hồi sinh.
- `ResetHealth(maximum)`: thao tác spawn/restart rõ ràng. Với enemy, gọi sau Instantiate bằng Health lấy từ CSV.
- Giá trị NaN, Infinity, 0 hoặc âm bị từ chối bằng `ArgumentOutOfRangeException` để phát hiện cấu hình/call site lỗi.
- Không gọi reset/revive từ callback `HealthChanged`; xử lý restart qua game flow sau khi hoàn tất thông báo.

### Interaction

`EchoesOfTheFog.Interaction.IInteractable` gồm `InteractionPrompt`, `CanInteract(GameObject interactor)`, `Interact(GameObject interactor)`.

Interactor là root Player. Implementation phải xử lý null, kiểm tra lại điều kiện trong `Interact`, và không kích hoạt objective lặp. Việc đọc phím F và tìm mục tiêu thuộc phase gameplay sau.

### Cấu hình enemy

Nguồn duy nhất: `Assets/StreamingAssets/GameConfig.csv`.

`GameConfigLoader.LoadDefault()` đọc bằng `StreamReader` trên Windows; trả về dictionary chỉ đọc chứa `EnemyConfig` bất biến. Bootstrap tương lai sẽ tải một lần, bắt IOException/FormatException và báo lỗi rõ; không âm thầm dùng dữ liệu khác khi CSV lỗi.

Schema cố định:

```csv
Id,Health,MoveSpeed,Damage,DetectionRadius,AttackRange,AttackCooldown,SearchDuration
Runner,40,4.5,10,18,1.5,1,5
Tanker,200,2,30,12,2,2.5,7
```

Thông số ban đầu để prototype, chưa cân bằng gameplay. Khoảng cách tính theo Unity unit, thời gian tính bằng giây, tốc độ unit/giây. Tên Id không phân biệt hoa/thường. Dùng dấu chấm thập phân, dấu phẩy phân cột, không dùng quoted fields/comment. Cho phép dòng trắng; bắt buộc có Runner và Tanker, không trùng Id. Tất cả số phải hữu hạn và dương; AttackRange không lớn hơn DetectionRadius. Lỗi có số dòng; không trả cấu hình nửa chừng.

## Scene kiểm chứng

`Assets/Game/Scenes/Tests/AssetValidation.unity` có camera, đèn, nền/collider, tường kiểm tra, target Health, mẫu đá/cầu/nước của Flooded Grounds và cây placeholder riêng.

`ValidationTreePlaceholder.prefab` chỉ là cây blockout bằng primitive, **không phải cây gốc đã sửa**. Chín prefab TreeCreator gốc hiện thiếu mesh/TreeData; muốn khôi phục cần dữ liệu gốc còn nguyên hoặc chọn asset thay thế. Chưa sửa scene demo hoặc ghi đè prefab gốc.

`EchoesOfTheFog.Editor.Phase1Validation.Run` là entry point batch tường minh, không tự chạy khi import. Nó chỉ thêm nội dung vào scene kiểm chứng khi scene còn camera/đèn mặc định; không rebuild scene đã có marker Phase1. Khi cần thay layout đã tạo, chỉnh scene qua Editor và review diff.

Kiểm tra thủ công:

1. Mở AssetValidation; kiểm tra đá/cầu/nước không hồng, placeholder tree nhìn thấy, fog không che hết mẫu.
2. Bật collider gizmos và kiểm tra nền, tường, cầu. Phase 1 chưa có Player để kiểm thử di chuyển xuyên vật cản.
3. Mở Test Runner → EditMode, chạy `EchoesOfTheFog.EditModeTests`.
4. Kiểm tra Package Manager hiện đúng Cinemachine/AI Navigation. Không thay phiên bản Unity/pipeline.

Batch setup (đóng Unity trước):

```powershell
& 'D:\Unity Hub\Editor\6000.3.26f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\Unity Projects\EchoesOfTheFog' -executeMethod EchoesOfTheFog.Editor.Phase1Validation.Run -logFile 'Logs/Phase1/setup.log'
```

Batch EditMode tests: dùng `-runTests -testPlatform EditMode -testResults Logs/Phase1/editmode-results.xml` thay cho `-executeMethod` và bỏ `-quit` để test runner tự kết thúc.

## Git và phối hợp

- Giữ các thay đổi có sẵn của người dùng; Phase 1 không commit/stage tự động.
- Git LFS đã có; bổ sung `.tif`, `.TIF`, `.tiff` trước khi add bộ texture (~2.70 GB).
- Commit Assets cùng `.meta`, Packages manifest/lock, ProjectSettings cần thiết. Không commit Library/Temp/Logs/UserSettings.
- Commit nhỏ theo module; một người phụ trách chỉnh chính mỗi scene. Không đổi/move asset gốc hàng loạt.
- Model, animation Player/enemy/NPC, xe tải/generator/vũ khí và SFX gameplay vẫn cần chuẩn bị.

## Phạm vi tiếp theo

Phase 2 mới triển khai Player/camera/input gameplay. Phase 1 chưa tạo Player controller, combat, FSM, mission, menu hoặc Windows player build. Sau báo cáo Phase 1 phải chờ review trước khi tiếp tục.

## Kết quả thực thi

- Unity batch setup: exit code **0**, compile thành công, tạo scene/prefab/material bằng Unity Editor API.
- Unity EditMode Test Runner: **20/20 Passed**, 0 failed, 0 skipped. Kết quả máy: `Logs/Phase1/editmode-results.xml`.
- Ảnh render bằng camera/GPU: `Logs/Phase1/asset-validation.png`; chưa phải kiểm thử Play Mode hoặc Windows player build.
- Asset report: `Logs/Phase1/asset-validation.txt`. 9 cây gốc vẫn thiếu mesh. Các mẫu scene không thay thế việc kiểm tra toàn bộ asset pack.
- Warning `CS0618` tại MotionBlurComponent của Flooded Grounds vẫn còn. Chưa chỉnh code bên thứ ba chỉ để tắt warning.
- `ProjectSettings/ProjectSettings.asset` giữ thay đổi Both có sẵn từ trước; Unity tự cập nhật `.slnx` với các assembly/package mới.
- Nguồn tham khảo phiên bản: [AI Navigation cho Unity 6000.3](https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-safe/com-unity-ai-navigation), [Cinemachine](https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-safe/com-unity-cinemachine); đã kiểm tra metadata phiên bản trên Unity Registry trước khi thêm.

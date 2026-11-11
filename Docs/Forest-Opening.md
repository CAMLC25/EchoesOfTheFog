# Forest Edge — giới thiệu, nhặt súng và nhiệm vụ mở đầu

## Luồng chơi

1. Mở `Assets/Game/Scenes/Main/MainMenu.unity`, nhấn Play rồi chọn **Bắt đầu chơi**.
2. Bảng giới thiệu nêu bối cảnh Blackwood bị phong tỏa vì bào tử; Elena là chuyên viên quân y đi tìm mẫu dịch chiết, cứu em gái/người sống sót và tìm đường thoát bằng xe tải. Chọn **Vào Blackwood** hoặc **Bỏ qua** để vào màn rừng; Esc quay lại menu trước khi tải.
3. Trong `Level01_Forest`, Elena bắt đầu tay không. HUD nhiệm vụ: **Tìm khẩu súng ức chế sinh học**.
4. Tìm bàn tiếp tế có ánh đèn gần điểm xuất phát, lệch bên trái lối đi. Đến gần sẽ hiện **Nhấn F để nhặt súng ức chế**.
5. Sau khi nhặt Gun_31, nhiệm vụ đổi thành **Vượt qua khu rừng, tới cổng làng**. Điều khiển ngắm/bắn/nạp đạn và animation cầm súng hoạt động trở lại. Có thể tránh hoặc tiêu diệt Runner; nhiệm vụ không bắt buộc hạ cả hai.
6. Đi qua khu vực mục tiêu ngay sau cổng làng để hoàn thành chặng bìa rừng. Màn hoàn thành có nút **Trở về menu**. Đây chưa phải Victory của cả game và chưa tải Level 2 chưa được triển khai.

Guide trong menu đã bổ sung F để nhặt vũ khí. Nhặt đạn, cứu thương, máy phát điện, NPC, các level còn lại và Game Over/restart UI không thuộc bước này.

## Vũ khí và tương tác

PlayerEquipment trên prefab Player giữ trạng thái có súng và visual của súng. Prefab mặc định vẫn có súng để các scene Tests kiểm tra combat độc lập; riêng instance Player trong Level01_Forest có override `startEquipped=false` và visual tắt lúc chưa chơi. Không có thao tác thả súng/đổi nhiều loại súng trong bước này.

HitscanRifle kiểm tra trạng thái trang bị ở cả API bắn và nạp đạn; trước khi nhặt, HUD không hiện đạn/crosshair và camera không vào chế độ ngắm. Khi nhặt, súng có băng 30 viên và dự trữ 90 viên theo cấu hình prototype hiện tại. Không thể nạp lại đạn bằng cách nhặt cùng vật phẩm nhiều lần. Giữ chuột trái trước khi nhặt không gây phát bắn ngoài ý muốn: cần nhả rồi nhấn lại.

PlayerInteractor quét collider trong khoảng gần Player, chọn mục tiêu hợp lệ gần nhất trên layer Interactable và kiểm tra line of sight. F gọi IInteractable; lúc tương tác kiểm tra lại khoảng cách, vật cản, trạng thái điều khiển và HP. WeaponPickup cũng kiểm tra lại điều kiện trước khi gọi TryEquipWeapon rồi tắt vật phẩm trong scene. Không nhặt xuyên tường, từ xa, khi thả chuột/khóa điều khiển hoặc khi chết.

`Assets/Game/Prefabs/Items/WeaponPickup.prefab` dùng visual Gun_31 hiện có, đặt nằm trên bàn `Prop_SmallTable_A` của Flooded Grounds và có đèn chỉ dẫn. Bàn là instance trong scene, có collider và NavMeshObstacle carving để enemy tránh vật thể mới; không sửa prefab nguồn hoặc dựng lại toàn map.

## Animation và nhiệm vụ

Animator đã bổ sung nhánh locomotion tay không bằng các clip Idle/Walking/Running có sẵn; nhánh có súng vẫn giữ các clip rifle trước đó. Jump chọn theo trạng thái Armed. Upper-body rifle và IK không áp tư thế cầm súng khi visual chưa được trang bị. Root motion vẫn tắt; Rigidbody sở hữu chuyển động.

ForestMission có ba trạng thái: FindWeapon, ReachVillage, Completed. Sự kiện PlayerEquipment.WeaponEquipped chuyển mục tiêu thứ nhất sang thứ hai. ForestExitTrigger chỉ hoàn thành khi đúng Player đã nhặt súng và còn sống. Chạm mục tiêu nhiều lần không hoàn thành lặp. ForestMissionHUD hiển thị mục tiêu, khoảng cách thẳng tới đích, prompt và màn hoàn thành bằng TextMeshPro SDF.

Màn hoàn thành khóa điều khiển và tạm dừng thế giới; Trở về menu khôi phục thời gian và tải MainMenu. Nay có thêm nút **Vào làng** tải map Level02_Village đã dựng, xem [Level02-Village.md](Level02-Village.md). Màn Extraction chưa có. Tải lại màn rừng sẽ bắt đầu lại với Player tay không, vật phẩm súng và nhiệm vụ ban đầu.

## Tích hợp và API

- `PlayerEquipment.HasWeapon`, `TryEquipWeapon()` và event `WeaponEquipped`: trạng thái trang bị dùng chung cho combat, camera, animation và mission.
- `PlayerInteractor.CurrentPrompt` / `TryInteract()`: chọn và thực hiện IInteractable, dùng action Interact/F hiện có; không đổi binding dự án.
- `ForestMission.Objective`, `ObjectiveChanged`, `CurrentTarget`: đầu ra cho HUD và tích hợp sau này.
- `OpeningFlowSetup.Apply`: công cụ Editor batch tạo các phần còn thiếu, cập nhật prefab/animation và instance Main. Đóng Unity trước khi chạy. Các bản sao trước thay đổi nằm trong `.utmp/Backups/BeforeOpeningFlow`.

Chưa có clip animation nhặt súng chuyên dụng; thao tác F trang bị tức thời rồi chuyển tư thế, không giả lập clip chưa được cung cấp. Chưa commit/push.

## Kiểm chứng

Kiểm thử PlayMode gồm intro/skip/Esc, đi vào rừng tay không, khóa ngắm/bắn/nạp đạn trước pickup, F ở xa hoặc bị che, nhặt một lần, giữ chuột trước khi nhặt, mở khóa bắn sau nhặt, điều kiện cổng làng, hoàn thành một lần, trở về menu/khôi phục thời gian, tải lại và Player chết. Bộ kiểm thử Player, jump, combat, Runner và đường đi trước đó cũng chạy lại.

Kết quả máy và ảnh nằm trong `Logs/OpeningFlow`; ảnh giới thiệu nằm trong `Logs/MainMenu/Story_*`. Ảnh render hỗ trợ kiểm tra hiển thị; vẫn cần chơi thử thủ công cảm giác tìm vũ khí và giao tranh trên tuyến đường.

Kết quả: **38/38 PlayMode** và **23/23 EditMode** pass. Sau khi chỉnh nhãn tải màn chơi và cách chụp UI trên nền 3D, **9/9 kiểm thử menu/nhiệm vụ** tiếp tục pass. Đã xem ảnh intro ở 1920×1080 và 1360×552, prompt nhặt súng, sau trang bị và màn hoàn thành. Bản kiểm tra nhiệm vụ dùng vị trí Player được điều khiển để cô lập điều kiện pickup/trigger; bộ walkthrough kiểm tra di chuyển và bộ Runner kiểm tra giao tranh vẫn chạy riêng trong lượt hồi quy đầy đủ.

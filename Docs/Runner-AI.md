# Runner — FSM, NavMesh và animation

## Chạy thử

- Mở `Assets/Game/Scenes/Tests/RunnerTest.unity` để thử Runner trong arena greybox có tường và thùng chắn.
- Mở `Assets/Game/Scenes/Main/Level01_Forest.unity` để chơi đoạn rừng–làng hiện tại với năm Runner tuần tra. Ba Runner bổ sung nằm từ sau cầu tới gần cổng làng; xem [Pause-and-GameOver.md](Pause-and-GameOver.md). Scene này dùng bố cục của AssetValidation và có NavMesh hoạt động. AssetValidation vẫn là scene kiểm tra Player/environment riêng.
- Màn Forest chính hiện khởi đầu tay không; nhặt súng gần điểm xuất phát bằng F trước khi bắn. Luồng giới thiệu và nhiệm vụ xem [Forest-Opening.md](Forest-Opening.md). RunnerTest vẫn có súng sẵn để kiểm tra enemy độc lập.
- Điều khiển Player giữ nguyên: WASD, Shift, Space, chuột phải ngắm, chuột trái bắn, R nạp đạn. Tiến lại gần trong hướng nhìn của Runner, nổ súng hoặc chạy nhanh để kích hoạt giao tranh.
- Gun_31 gây 25 damage/viên; Runner có 40 HP nên hai phát trúng sẽ hạ gục. Runner gây 10 damage/đòn. Dừng Play rồi chạy lại để reset HP, enemy và đạn.

Nếu Scene View hiện vùng NavMesh màu xanh, ẩn overlay NavMesh/Gizmos để xem môi trường. Giữ object Navigation hoạt động trong các scene Runner, vì enemy cần dữ liệu này để di chuyển.

## Cấu hình và hành vi

Thông số cốt lõi đọc từ `Assets/StreamingAssets/GameConfig.csv` bằng GameConfigLoader hiện có: Runner 40 HP, tốc độ đuổi 4.5 m/s, damage 10, bán kính nhìn 18 m, tầm đánh 1.5 m, cooldown tối thiểu 1 giây và tìm kiếm 5 giây. Dữ liệu Tanker trong CSV được giữ, chưa triển khai Tanker.

`Assets/Game/Data/Enemies/RunnerTuning.asset` chứa thông số phụ: tốc độ tuần tra 1.4 m/s, góc nhìn 120°, khoảng kiểm tra giác quan 0.15 giây, cập nhật đường đi 0.25 giây, thời gian chờ waypoint, hit stun và timing hai đòn đánh. Có thể chỉnh trong Inspector.

FSM C# có Idle, Patrol, Chase, Attack, Search, HitReact, Dead. Detect là kiểm tra cảm biến dẫn tới chuyển trạng thái, không giữ một trạng thái chờ riêng.

- Patrol đi qua waypoint rồi chờ ngắn; không có waypoint thì Idle.
- Phát hiện bằng khoảng cách, góc nhìn và raycast vật cản. Khi đang đuổi/đánh, Runner tiếp tục theo dõi bằng khoảng cách và line of sight.
- Mất dấu: đi tới vị trí cuối nhìn/nghe thấy và Search trong thời gian giới hạn. Không cập nhật đích bằng vị trí hiện tại của Player khi đã mất dấu.
- Tiếng súng phát sự kiện nghe trong 26 m; chạy nhanh phát bước chân trong 12 m, tối đa một lần mỗi 0.45 giây. Đây là sự kiện gameplay, chưa phải âm thanh phát ra loa. Đi bộ không phát sự kiện sprint.
- Nghe tiếng qua vật cản dẫn tới Search tại vị trí phát tiếng, không tự coi là đang nhìn thấy Player.
- Đòn đánh có windup. Tại thời điểm impact, kiểm tra lại tầm, góc đánh và vật cản rồi chỉ gây một lần sát thương. Hai clip attack luân phiên, được chỉnh tốc độ phát theo duration 1.0/1.2 giây. Khoảng cách giữa hai đòn luôn ít nhất bằng cooldown CSV.
- Nhận đạn còn sống: HitReact, dừng di chuyển và hủy đòn đang chuẩn bị. Hết HP: Dead, dừng agent, hủy tấn công, tắt collider và phát animation chết; không ragdoll.
- Target null/chết/không hoạt động hoặc không có NavMesh: dừng an toàn, không gọi API điều hướng trên agent chưa được gắn NavMesh. Spawn chỉ snap vào NavMesh gần đó trong khoảng cấu hình.

## Model và animation

Prefab `Assets/Game/Prefabs/Enemies/Runner.prefab` dùng model Runner đã import, rig Humanoid hợp lệ, chiều cao visual 1.8 m. Root có Health, NavMeshAgent, CapsuleCollider, Rigidbody kinematic, EnemyPerception, RunnerBrain và RunnerAnimationDriver. Enemy dùng layer Enemy; raycast súng vẫn trúng capsule và gọi IDamageable/Health hiện có.

Animator `Assets/Game/Animations/Runner/Generated/Runner.controller` dùng đủ 7 clip: Zombie Idle, Walk, Run, Attack 1, Attack 2, Reaction Hit, Death. Idle/Walk/Run loop; các clip còn lại chạy một lượt. Blend Tree locomotion đọc vận tốc thực của agent. Animator root motion tắt để NavMeshAgent sở hữu di chuyển; FBX gốc giữ nguyên, chỉ sửa importer/metas. Hai texture normal map được cấu hình đúng loại texture.

## API tích hợp

- `RunnerBrain.SetTarget(Health)` dùng để gán Player khi spawn hoặc thay Player trong tương lai. Prefab tìm Player theo tag một lần lúc Start nếu scene chưa gán target.
- `RunnerBrain.SetPatrolPoints(Transform[])` cấu hình tuyến tuần tra. Scene mẫu đã có waypoint và target.
- `RunnerBrain.State`, `NavigationReady`, `Config` dùng để debug; `StateChanged` và `AttackStarted` phục vụ animation/tích hợp.
- `Health.Died` vẫn là event chung để mission/spawner đăng ký sau này.
- `PlayerNoiseEmitter` trên prefab Player chuyển shot event và sprint thực tế thành `GameplayNoise.Emitted`. Emitter và Runner hủy đăng ký khi disable để không giữ listener cũ.
- `RunnerSetup.Apply` là công cụ Editor batch chạy tường minh khi Unity đã đóng, không tự chạy lúc mở project. Nó tạo các asset/scene còn thiếu và giữ những prefab/scene đã tồn tại. Backup Player trước thay đổi nằm ở `.utmp/Backups/BeforeRunner`.

## Phạm vi và kiểm chứng

Đây là enemy Runner mẫu. Chưa thêm Tanker, wave spawner, nhiệm vụ, âm thanh hú/gọi bầy, ragdoll, Game Over/restart UI hoặc cân bằng toàn game. Chưa đo FPS và chưa build Windows.

Test PlayMode có kiểm tra waypoint, giữ model đúng với agent, FOV, vật cản, tiếng súng/chạy, Search timeout, đi vòng quanh tường, cooldown, né trong windup, cản đòn bằng vật thể, target chết/null, thiếu NavMesh, hai phát Gun_31 hạ Runner và scene rừng hai Runner. Bộ Player/jump/combat/map walkthrough trước đó cũng chạy lại. Kết quả và ảnh pose nằm trong `Logs/Runner` (không commit Logs).

Kết quả: **29/29 PlayMode**, **23/23 EditMode** pass, 0 failed. Đã xem ảnh Runner trong arena và map rừng; Avatar valid/human, bảy clip Humanoid và các đường đi tuần tra/đường tới Player đều được kiểm tra. Không thay đổi package hoặc môi trường gốc Flooded Grounds.

Review thủ công: thử lùi sau tường khi bị đuổi, chạy thay vì đi bộ sau lưng Runner, né đòn bằng cách lùi/nhảy, và đánh hai Runner liên tiếp trên đường rừng. Ảnh render và kiểm thử tự động không thay thế đánh giá cảm giác chiến đấu. Chưa commit/push thay đổi Runner.

# Pause, Game Over và Runner bổ sung

Áp dụng trong `Assets/Game/Scenes/Main/Level01_Forest.unity`.

- **Esc** mở menu tạm dừng: Tiếp tục, Chơi lại, Về menu. Esc lần nữa tiếp tục; chuột được mở khi hiện menu và khóa lại khi tiếp tục.
- Khi Player hết máu, hiện Game Over với Chơi lại và Về menu. Esc không hồi sinh hoặc tiếp tục màn đã thua.
- Chơi lại tải lại toàn bộ màn rừng: Player tay không, 100 máu, không cứu thương; súng, vật phẩm và Runner được khôi phục theo scene. Chưa có checkpoint.
- Về menu tải MainMenu. Các thao tác tải cảnh khôi phục thời gian trước khi rời scene; khóa nút trong lúc đang tải để tránh lệnh lặp.
- Hoàn thành chặng rừng vẫn dùng màn hoàn thành nhiệm vụ hiện có; Esc không gỡ trạng thái hoàn thành.

`ForestGameFlow` quản lý Playing / Paused / GameOver / Completed / Loading. Khi dừng, khóa điều khiển, mở chuột, đặt Time.timeScale = 0, ẩn HUD gameplay và giữ UI tương tác. Tiếp tục khôi phục tốc độ thời gian trước đó; click Tiếp tục không tự bắn súng. `ForestMissionHUD` dùng cùng bộ quản lý thời gian khi scene có ForestGameFlow, giữ fallback cho scene kiểm thử độc lập.

`PlayerInputReader.PauseRequested` chuyển action Esc cho menu khi được đăng ký. Trong scene test không có menu, Esc vẫn thả chuột như trước. Không nâng package hay đổi hệ input.

Thêm **3 Runner**, tổng **5** trong màn rừng chính. Ba nhóm tuần tra bên đường ở z = 20, 28, 44, cách tim đường 7–8 m. Mỗi Runner dùng cùng prefab, C# FSM và NavMeshAgent hiện có, có hai waypoint và kiểm tra đường NavMesh đầy đủ. Không đổi HP, sát thương hoặc bán kính phát hiện. Giữ hai Runner cũ và khoảng đầu màn an toàn; tiếng súng có thể thu hút các nhóm ở gần nhau, cần chơi thử để cân bằng độ khó thực tế.

Editor: `ForestGameFlowSetup.Apply`, chạy batch khi đã đóng Unity. Chỉ thêm UI và nhóm Runner mới, không dựng lại môi trường. Bản sao trước thay đổi: `.utmp/Backups/BeforePauseGameOver`.

Kiểm thử và ảnh render nằm trong `Logs/Pause`. Chưa commit/push.

Kết quả cuối: **47/47 PlayMode pass** (`final-results.xml`). Kiểm tra Esc dừng/tiếp tục, đứng yên của Player/Runner khi dừng, khóa bắn/chữa trị, khôi phục time scale, không bắn do click Tiếp tục, Game Over ưu tiên hơn Pause, Esc không hồi sinh, Chơi lại bằng Enter khôi phục màn, Về menu từ cả hai menu và không tiếp tục màn đã hoàn thành. Bộ cũ kiểm tra di chuyển/jump, đạn/cứu thương, combat, FSM, nhiệm vụ, menu và đường NavMesh của cả 5 Runner cũng pass. Đã xem ảnh Pause/GameOver ở 1360×552; nhãn hướng dẫn Esc rút gọn thành “Tạm dừng” để không tràn chữ. Chưa đánh giá cân bằng giao tranh bằng chơi thử thủ công.

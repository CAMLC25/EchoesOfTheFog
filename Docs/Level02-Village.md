# Level 02 — Broken Bridge & Abandoned Village

Scene: `Assets/Game/Scenes/Main/Level02_Village.unity`.

Đây là bản map chơi thử cho màn 2 trong hai tài liệu gốc: cầu gỗ và làng bỏ hoang, bật nguồn điện để mở đường tới trạm y tế. Môi trường dùng prefab Flooded Grounds; không chỉnh scene demo hoặc model/material nguồn.

## Tuyến đường

| Vị trí | Tọa độ tham khảo | Vai trò |
|---|---|---|
| Spawn và cổng làng | (0, 0, -15) | Khoảng khởi đầu an toàn, nhìn vào phố chính |
| Quảng trường | (0, 0, 16) | Nhà dân, kiosk, xe hỏng, chỗ né và bàn tiếp tế |
| Sân máy phát | (-18, 0, 25) | Rẽ trái từ phố chính, F bật nguồn hạ cầu |
| Kênh và cầu | (0, 0, 40) | Kênh rộng 8 m, cầu rộng 4.2 m; bắt đầu dựng lên |
| Khu dân cư phía bắc | (0, 0, 58–82) | Runner, đường chính và lối vòng bên nhà |
| Trạm y tế | (0, 0, 90–100) | Nhà có dấu thập đỏ, sân tiếp cận, vị trí NPC tương lai |

Cầu hạ trong 2.5 giây sau tương tác, sau đó bật collider và bỏ chặn NavMesh. Tạm dừng sẽ dừng chuyển động cầu. Ngã xuống kênh sâu gây Game Over; không thể đi dưới nước để bỏ qua cầu. Có biên va chạm để tránh ra khỏi vùng chơi.

Map có **4 Runner**, **3 bàn tiếp tế** (mỗi bàn một hộp 30 đạn và một cứu thương), Player Elena/Gun_31, third-person camera, HUD nhiệm vụ, Pause và Game Over. Runner giữ nguyên FSM/NavMeshAgent/config, không có ML-Agents.

## Chơi thử

- Chơi màn rừng tới cổng làng, chọn **Vào làng** trên màn hoàn thành; hoặc mở Level02_Village rồi Play.
- WASD, Shift, Space, chuột/R, F và E như màn 1; Esc tạm dừng.
- Tìm sân máy phát có đèn vàng bên trái, F bật nguồn, quay lại cầu rồi đi tới biển trạm y tế.
- Chơi lại trong màn 2 tải lại chính màn 2. Đây là điểm bắt đầu màn mới với 100 máu, súng và 90 đạn dự trữ, 0 cứu thương; chưa chuyển số lượng/HP từ màn 1.

## Phạm vi đã làm và phần còn thiếu

Nhà dân/cabin, kiosk, xe hỏng, bàn, hàng rào, cột, cây, bụi và đá dùng asset thật. Trạm y tế là cabin với biển/dấu thập đỏ; sân trước đã có vị trí tiếp cận, chưa bố trí nội thất bệnh viện hoặc dân làng. Cầu dùng art ván gỗ của asset và collider riêng.

**Máy phát là prop tạm** dựng bằng hình khối có màu, lưới tản nhiệt và công tắc F hoạt động; cần thay bằng model máy phát hoàn thiện sau. Biển không thay thế NPC. Có model Tanker trong project nhưng chưa có animation/AI tích hợp cho Tanker ở màn này; marker dành cho trận đánh nằm gần sân máy phát. Chưa có giải cứu, trao chìa khóa, mẫu bào tử, NPC đi theo hoặc màn 3. Đến trạm y tế chỉ xác nhận đã tới vị trí, không tuyên bố hoàn thành chuỗi giải cứu.

Ánh sáng xám lạnh, fog tuyến tính, đèn vàng dẫn đường và đèn trạm y tế bật sau khi cấp điện. Đây là bố cục môi trường đầu tiên để chơi thử; mật độ trang trí, nhịp giao tranh và ánh sáng còn cần review thủ công.

## Kỹ thuật và tái tạo

- `VillageRoute`, `VillagePowerSwitch`, `VillageWaterHazard`: điều khiển tuyến map/cầu, tương tác chung IInteractable và vùng nước nguy hiểm.
- `ForestGameFlow` được dùng lại với mission tùy chọn; Restart tải scene hiện tại. Nút Vào làng chỉ tải khi màn rừng đã hoàn thành.
- Build Settings: MainMenu → Level01_Forest → Level02_Village. Scene test không được thêm vào build.
- `VillageAssetSurvey.Inspect`: kiểm tra bounds/collider prefab thực tế.
- `VillageMapSetup.Build`: dựng scene mới; từ chối ghi đè scene đã tồn tại. `Polish`: bước chỉnh môi trường sau dựng, chạy một lần. `Capture`: chụp các góc nhìn bằng GPU.
- Mesh mặt đất, vật liệu tùy chỉnh và NavMesh riêng trong `Assets/Game/Art/Village`; art gốc được tham chiếu.
- Sao lưu màn rừng trước nối cảnh tại `.utmp/Backups/BeforeVillage`; sao lưu bản làng trước polish tại `.utmp/Backups/BeforeVillagePolish`.

Ảnh render và kết quả máy nằm trong `Logs/Village` (Git ignore). Chưa commit/push.

Kết quả cuối: **51/51 PlayMode pass** (`final-results.xml`), gồm 47 bài cũ và 4 bài màn làng. Kiểm tra chuyển màn bằng nút Vào làng, 4 Runner có NavMesh/target hợp lệ, đường qua kênh bị chặn lúc cầu dựng, F cấp điện một lần, Pause dừng chuyển động cầu, NavMesh thông sau hạ cầu, Player thực sự đi qua deck với collider và tới trạm y tế, rơi xuống kênh → Game Over, Restart giữ đúng màn 2 và reset máy phát. Đã xem ảnh các góc môi trường, GameView_Start và GameView_Clinic; HUD được kiểm tra không tràn chữ. Kiểm thử đi bộ dùng input thực, phần tới trạm y tế cuối bài dùng teleport để cô lập trigger khoảng cách; chưa phải một lượt chơi thủ công hoàn chỉnh từ cổng tới trạm.

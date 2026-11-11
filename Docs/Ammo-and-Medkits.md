# Đạn và cứu thương

Trong `Scenes/Main/Level01_Forest`, hai bàn tiếp tế nằm bên đường trước cầu và sau cầu (z = -6 và 26). Mỗi bàn có một hộp 30 đạn và một cứu thương. Màn chơi vẫn bắt đầu tay không, không có cứu thương.

- **F** nhặt vật phẩm gần nhất, cùng hệ tương tác với súng. Không nhặt xuyên vật cản, khi đã chết hoặc đang thả chuột bằng Esc.
- Đạn vào kho dự trữ, không tự điền băng. Nhặt súng trước khi nhận đạn; dùng **R** để nạp như trước. Băng 30, dự trữ ban đầu 90, tối đa 180.
- Nếu chỉ còn chỗ cho một phần đạn, vật phẩm giữ nguyên phần còn lại để nhặt sau. Kho đầy không làm mất vật phẩm.
- Cứu thương mang tối đa 3; có thể nhặt khi máu đầy. **E** hoặc D-pad Up dùng một túi, hồi tối đa 40 máu, không vượt máu tối đa. Giữ E không dùng liên tục. Không tiêu hao khi máu đầy; không hồi sinh người đã chết.
- HUD TextMeshPro SDF góc dưới phải hiển thị số lượng và phím dùng. Thông báo ngắn xác nhận nhặt/dùng. Màn hoàn thành ẩn HUD này.

Model nguồn được giữ tại `Assets/Game/Art/Items/Ammo/Ammo_shotgun.fbx` và `Medkit/FirstAidKit.fbx`. FBX không bị chỉnh mesh; importer đặt thành prop tĩnh, không nhập animation. Medkit dùng material Standard có sẵn; Ammo dùng material wrapper `AmmoPickupPalette.mat` gán texture `Diffuse_palette_2.jpg` đã được cung cấp, giữ nguyên material nguồn. Prefab wrapper nằm tại `Assets/Game/Prefabs/Items/AmmoPickup.prefab` và `MedkitPickup.prefab`; kích thước lớn nhất lần lượt 0.28 m và 0.36 m. Tên file Ammo_shotgun chỉ là model đại diện đạn trong prototype, chưa có phân loại cỡ đạn hay súng shotgun.

`SupplyPickup` thực thi `IInteractable`, giữ lượng còn lại và chỉ giảm khi người chơi nhận thành công. `PlayerSupplies` quản lý cứu thương; `WeaponAmmo` quản lý giới hạn dự trữ; `PlayerSuppliesHUD` chỉ hiển thị. Chưa có animation nhặt hoặc chữa trị, thao tác diễn ra tức thời.

`SuppliesSetup.Apply` chạy bằng Editor batch khi Unity đã đóng, cập nhật prefab người chơi, prefab vật phẩm, hai bàn tiếp tế và hướng dẫn menu. Không dựng lại toàn bộ map. Bản sao scene/prefab trước thay đổi ở `.utmp/Backups/BeforeSupplies`.

Kiểm thử và ảnh render nằm trong `Logs/Supplies` (được Git ignore). Chưa commit/push.

Kết quả: **43/43 PlayMode** pass cho toàn bộ bộ kiểm thử. Sau khi hoàn thiện palette, bàn lớn và binding E lưu trong input asset, **9/9 kiểm thử vật phẩm/menu** pass, gồm F trên bàn thật, chặn tương tác qua vật cản, nhận đạn một phần rồi nhặt tiếp sau khi nạp, giới hạn mang, dùng E một lần khi giữ phím, không dùng khi đầy máu/chết/thả chuột. Đã xem ảnh `Station.png`: cả hai model nằm trên bàn, hộp đạn có màu palette, HUD và thông báo nhặt hiển thị đủ dấu tiếng Việt. Ảnh được chụp trong kiểm thử với góc camera cố định; cảm giác chơi vẫn cần kiểm tra thủ công.

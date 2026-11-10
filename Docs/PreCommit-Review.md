# Kiểm tra trước commit — 11/11/2026

Đã đối chiếu chỉ đọc bản asset trong project với package cache gốc:

`C:/Users/blvck/AppData/Roaming/Unity/Asset Store-5.x/Sandro T/3D ModelsEnvironments/Flooded Grounds.unitypackage`

## Kết quả SHA-256

Chi tiết từng file: `FloodedGrounds-SourceComparison.csv` trong cùng thư mục. So sánh byte của asset và metadata riêng biệt; `Different` không đồng nghĩa với asset bị hỏng.

| Loại | Giống byte nguồn | Khác byte nguồn | Thiếu |
|---|---:|---:|---:|
| Asset | 740 | 378 | 0 |
| Metadata `.meta` | 103 | 1123 | 0 |

Tất cả 342 FBX, 136 TIF, 110 PNG, 4 TGA, 8 MP3 và 30 shader giống SHA-256 nguồn. 81/83 script C# giống nguồn.

Hai script khác nguồn:

- `PostProcessing/Editor/PropertyDrawers/MinDrawer.cs`: ghi rõ namespace `UnityEngine.PostProcessing.MinAttribute`.
- `PostProcessing/Editor/Utils/CurveEditor.cs`: đổi tên enum API cũ `repaint/mouseDown/keyDown` thành `Repaint/MouseDown/KeyDown`.

Đây là các thay đổi tương thích API; không suy ra được tác giả hoặc thời điểm thay đổi chỉ từ phép đối chiếu này.

318 prefab, 50 material, 2 scene, 2 animation, 2 controller và 2 asset khác nguồn. Prefab cây mẫu gốc chứa định dạng nhị phân với marker Unity **5.2.2f1**, còn project hiện dùng YAML. Việc chuyển đổi serialization/import có thể làm đổi hash. Không có snapshot trước Phase 1 nên không thể quy toàn bộ khác biệt cho một lần import cụ thể, cũng không thể khẳng định các file trong thư mục asset giống byte package gốc.

Project có thêm hai file LightingSettings và `.meta` trong thư mục Scenes không có trong package. Chín prefab TreeCreator thiếu mesh/TreeData vẫn là vấn đề chưa giải quyết. Không phục hồi/ghi đè dữ liệu asset trong lượt kiểm tra này.

## Sẵn sàng review

- `git diff --check`: không có lỗi whitespace.
- Các file mới trong Assets có `.meta` đi kèm.
- `.gitattributes` đã có Git LFS cho TIF; `.tif` mẫu được Git xác nhận dùng filter `lfs`.
- Phase 1 trước đó: Unity compile thành công, 20/20 EditMode tests pass. Lượt này chỉ thêm báo cáo, không sửa runtime hoặc chạy lại tests.
- Giữ nguyên các thay đổi có sẵn, chưa stage/commit/push.
- Script so sánh và mẫu trích xuất nằm trong `Temp`, được ignore; không cần đưa vào commit.

## Lệnh để người dùng tự commit

Chạy tại thư mục project. Tách asset import khỏi foundation để lịch sử dễ review. Asset import là trạng thái sau import Unity hiện tại, không phải bản sao byte nguyên gốc.

```powershell
git add -- .gitattributes Assets/Flooded_Grounds.meta Assets/Flooded_Grounds Assets/_TerrainAutoUpgrade.meta Assets/_TerrainAutoUpgrade
git diff --cached --stat
git commit -m "Import Flooded Grounds environment and track TIF textures with LFS"

git add -- Assets/Game Assets/StreamingAssets.meta Assets/StreamingAssets Packages/manifest.json Packages/packages-lock.json ProjectSettings/ProjectSettings.asset EchoesOfTheFog.slnx Docs
git diff --cached --stat
git commit -m "Add phase 1 foundation, CSV config and validation scene"

git status --short
```

Texture TIF khoảng 2.70 GB sẽ được đưa vào LFS khi add. Các lệnh trên chưa push; việc đẩy lên remote sẽ tải các object LFS theo cấu hình remote của bạn.

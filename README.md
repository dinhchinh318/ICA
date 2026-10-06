# Luma Reef

Game arcade bắn cá offline cho **Unity 6000.6.4f1 / URP 2D**, thiết kế **dọc 9:16, Canvas 1080×1920**. Nền, cá, súng, đạn, icon, VFX và khung UI là asset AI riêng cho Luma Reef; âm nhạc/SFX được tổng hợp bằng code. Xu và gem chỉ là tiền ảo; không có thanh toán hoặc cash-out.

## Chạy

1. Mở dự án bằng Unity Hub, chọn đúng Unity 6. Có sẵn URP, Input System và uGUI trong `Packages/manifest.json`.
2. Mở `Assets/Scenes/BootScene.unity`, bấm **Play**, chọn **CHƠI NGAY**. Chọn Game View 9:16 hoặc 1080×1920; Windows mặc định mở 540×960 để vừa màn hình laptop.
3. Di chuột để ngắm, giữ chuột trái hoặc giữ/kéo ngón tay để bắn. AUTO tự bắn, LOCK theo cá được chọn gần con trỏ. `-` / `+` đổi cược; SHOP mua/chọn súng.
4. Sáu nút bên phải: FREEZE, RAPID, LOCK, POWER, ARC, BOMB. Kỹ năng dùng cooldown; SHOP có nạp cooldown bằng gem ảo.
5. `Esc` mở/tắt pause. `F3` mở debug trên Editor và Development Build.

Nếu mở project trên máy mới mà asset demo chưa được tạo, chạy **Luma Reef → Generate or Repair Project** một lần. Công cụ này tái lập dữ liệu cân bằng demo; sao lưu các asset đã chỉnh tay trước khi chủ động chạy lại. Mọi tham chiếu sprite, data, audio, VFX được gán tự động. Scene rỗng là có chủ đích: `ReefApp` dựng composition root/pool/UI một lần khi ứng dụng khởi động, sau đó chuyển scene bất đồng bộ qua màn hình loading.

Build Windows: **Luma Reef → Build Windows Development**. Kết quả: `Builds/Windows/LumaReef.exe`. Phải giữ file EXE cạnh `LumaReef_Data`, `UnityPlayer.dll` và các thư mục hỗ trợ.

## Nội dung

- 28 loài, gồm 6 nhỏ, 7 vừa, 6 lớn, 5 đặc biệt, 4 boss; giới hạn 35 cá và 1 boss.
- 8 súng với 8 sprite AI và 8 loại đạn AI riêng, 3 mức cược cho mỗi súng, đạn nảy một lần, kiểm tra va chạm quét theo đoạn di chuyển, lưới AoE.
- 21 đường cubic Bézier thuộc 7 kiểu; 6 đội hình; wave 25 cá, boss theo timer.
- Cá đặc biệt: nổ AoE, điện AoE, điện dây chuyền, thưởng kho báu và thưởng xu vàng. Chuỗi hiệu ứng được xử lý bằng hàng đợi có giới hạn.
- Boss có HP, cảnh báo, thanh máu, nhạc riêng, hit pause 65 ms, slow motion cục bộ và coin shower.
- Pool cá, đạn, coin, chữ thưởng và particle/net/impact/explosion. Đạn có TrailRenderer, recoil và muzzle flash; lưới, nổ, băng, burst xu có sprite AI. Pool đầy sẽ bỏ hiệu ứng hoặc hoãn phát bắn, không tăng dung lượng trong Update.
- Menu, room offline, shop, 6 daily quest, daily reward, achievement, mail giới thiệu, settings, save JSON có bản sao dự phòng.
- Cosmetics mẫu có hiệu ứng nhìn thấy: màu đạn, avatar, khung avatar, màu hiệu ứng bắt cá. FRIENDS hiển thị trạng thái chưa kết nối server.

## Các phase và vị trí file

| Phase | File chính trong `Assets/Scripts/` | Thiết lập / cách kiểm tra |
|---|---|---|
| 1 | `Core/GameManager.cs`, `ReefInput.cs`, `FixedPool.cs`, `Gun/Cannon.cs`, `Bullet/BulletActor.cs`, `Data/GunData.cs`, `BulletData.cs` | Compiler độc lập; camera, input, cannon và pool được dựng trong composition root. |
| 2 | `Data/FishData.cs`, `RoomData.cs`, `Path/BezierPath.cs`, `Fish/FishActor.cs`, `Spawn/SpawnDirector.cs` | Chỉnh loài trong `Assets/ScriptableObjects/Fish`, đường bơi trong `Paths`; sprite được gán sẵn. |
| 3 | `Economy/Wallet.cs`, `Gameplay/CombatResolver.cs`, `VFX/RewardViewPool.cs` | Cược được snapshot khi bắn. Reward = snapshot bet × multiplier; coin animation chỉ là presentation, không phát thưởng lần hai. |
| 4 | `Gun/GunLoadout.cs`, `Data/SkillData.cs`, `Skill/SkillController.cs`, `Gameplay/SpecialFishController.cs` | Chỉnh thông số trong `Guns`, `Skills`, `LanternShoals.asset`. |
| 5 | `VFX/EffectsPool.cs`, `CameraFXManager.cs`, `Audio/AudioManager.cs`, `Data/AudioLibrary.cs` | SFX/music trong `Assets/Audio`; hai bus Music/SFX trên `Reef.mixer`; pool Particle System có giới hạn hạt. |
| 6 | `Core/ReefApp.cs`, `UI/ReefUI.cs`, `Save/JsonSaveStore.cs`, `Quest/QuestController.cs`, `Data/QuestData.cs`, `ReefCatalog.cs` | Mở BootScene và Play; không phải gán inspector thủ công. |
| 7 | `Editor/ReefProjectBuilder.cs`, `Editor/ReefValidation.cs`, `Tools/Compile.ps1` | Generate, domain checks, build, sau đó chạy smoke test standalone. |

## Bộ asset AI và độ nét

`Assets/Art/AI/` chứa PNG nguồn: `lobby.png`, `arena.png`, `fish-atlas.png`, `cannons-atlas.png`, `ui-atlas.png`, `projectiles-atlas.png`, `effects-atlas.png`, `ui-kit-atlas.png`. Tổng cộng 2 nền, 28 cá, 8 súng, 16 icon, 8 đạn, 8 VFX, 9 skin UI. Alpha gốc được giữ nguyên; import dùng vùng connected-component cho sprite riêng và ô lưới cho VFX. Khung/nút dùng nine-slice và nhấn co giãn, chữ được render bằng font riêng thay vì bake vào ảnh.

Texture không nén/mipmap trong bản Windows mẫu để tránh mất viền ở kích thước nhỏ; camera giữ đúng khung 9:16. Nguồn PNG được giữ đúng độ phân giải do công cụ xuất, không giả định tất cả là 4K. Prompt và nguồn tạo ở `GenerationPrompts.md` / `PremiumPrompts.md`. Menu **Luma Reef → Apply AI Portrait Art** gắn lại asset AI mà không tái lập cân bằng demo.

## Dữ liệu và cân bằng

`Assets/Resources/ReefCatalog.asset` là đầu vào duy nhất của runtime. Multiplier, xác suất bắt, tốc độ, HP, kích thước, trọng số spawn, mức cược, tốc độ đạn/súng, cooldown, timer và pool capacity nằm trong ScriptableObject. Sửa asset qua Inspector rồi Play. `coinReward` là giá trị tham khảo, reward thật luôn dùng `bet × multiplier`.

Xác suất thường: `1 - (1 - killChance)^power`. Boss dùng HP thay cho RNG. Một volley nhiều nòng trừ một mức cược, chia capture power cho các viên để tránh tự nhân hiệu quả theo số nòng. Thông số demo ưu tiên nhìn thấy vòng chơi; chưa phải mô hình kinh tế cho sản phẩm live.

Khi thay art, thay `FishData.sprite` hoặc sprite trong catalog/gun/bullet data. Cá nhìn sang phải ở local +X. Đạn nhìn lên +Y. Toàn bộ actor cache component. Animation hiện tại dùng biến dạng nhẹ bằng transform; `AnimationClip` mẫu được giữ trong data để mở rộng sang animation frame.

## Save và multiplayer tương lai

Save: `Application.persistentDataPath/luma-reef-save.json`, `.bak` và file tạm khi ghi. Windows thường nằm ở `%USERPROFILE%/AppData/LocalLow/Lantern Workshop/Luma Reef/`. Lưu định kỳ nếu có thay đổi, lúc pause, đổi menu và thoát. Ngày quest/daily dùng UTC, ngăn nhận lại cùng ngày hoặc quay lùi ngày đã nhận.

`ISaveStore`, `IWallet`, `ICombatAuthority` là các điểm thay implementation. Muốn server-authoritative, server phải sở hữu ví, RNG, HP, thời gian cooldown, xác thực phát bắn và id sự kiện chống thưởng trùng. Client chỉ gửi lệnh aim/fire/skill và dựng hình từ event/snapshot. Bản này chưa có networking, authentication hay chống sửa đồng hồ/save; không dùng các implementation offline làm authority cho server.

## Kiểm chứng

- `Tools/Compile.ps1`: kiểm tra C# bằng Roslyn/reference của bản Unity cài trên máy. Đây không thay thế Unity import/build.
- **Luma Reef → Run Domain and Content Checks**: asset references, đường bơi, ví, khóa súng/cược, skill cooldown, quest/daily, save backup, quét va chạm, snapshot cược, thưởng một lần, cap cá/boss và wave.
- **Luma Reef → Build Windows Development** chạy domain checks trước khi build.
- `Builds/Windows/LumaReef.exe --reef-smoke -force-d3d11 -screen-width 540 -screen-height 960 -logFile smoke.log`: tự kiểm tra runtime, chạy auto fire và xuất `lobby-smoke.png`, `gameplay-smoke.png`, `shop-smoke.png` ở 1080×1920 bằng render request; save test nằm riêng trong temporary cache, không đụng save người chơi. Log kết thúc bằng `LUMA_REEF_SMOKE_PASS` nếu không có error/exception/assert. Test kiểm tra ảnh không bị trống.

## Mobile và giới hạn xác nhận

Android: cài Android Build Support (SDK/NDK/OpenJDK) trong Unity Hub, mở Build Profiles → Android, giữ ba scene theo thứ tự Boot/MainMenu/Game; minimum API 26, portrait. Input đã hỗ trợ touch. Máy hiện tại chỉ có Windows Build Support nên chưa xuất APK. iOS cần module iOS và Xcode trên macOS.

Mục tiêu 60 FPS, đã dùng fixed pools và sprite atlas. Chưa thể cam kết 60 FPS trên máy Android tầm trung nếu chưa profile thiết bị thật. Gameplay tick không dùng LINQ hoặc tạo actor; UI số đếm/chữ thưởng còn tạo chuỗi khi cập nhật. Art/VFX là asset mẫu có thể thay thế, không phải bộ art production hoàn thiện. Các mục social/multiplayer hiện là trạng thái offline có ghi rõ trong UI.

Tham khảo API mixer khi xây công cụ Editor: [Unity C# reference](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Audio/Mixer/Bindings/AudioMixerController.cs). Reflection chỉ nằm ở công cụ Editor; runtime dùng API audio công khai.

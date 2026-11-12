# RayBPS4

[English](#english) | [Русский](#русский)

---

## English

**RayBPS4** is a fork of [DirectPackageInstaller](https://github.com/marcussacana/DirectPackageInstaller) (original author: marcussacana, GPL-3.0) with two extra features: **automatic deletion of the `.pkg` after it is sent to the PS4** and **torrent downloading (magnet link or `.torrent`)**.

> Both new features are experimental. The code was written without the ability to run it on a real console, so please test it on your own device.

### What the app does

**From the original project**
- Sends a `.pkg` to a PS4 from a PC or phone using a direct link or a local file, without manually downloading it on the console first.
- Works through a receiver on the console: **Remote Package Installer** or **GoldHEN Payload Server**.
- Accepts links through the services configured on the **Debrids** tab (AllDebrid, RealDebrid, DebridLink), as well as direct links and Google Drive.
- Platforms: Windows, Linux, macOS, Android.

**Added in this fork**

1. **Delete PKG after transfer** (Options menu)
   - After a file is sent through Remote Package Installer, the app polls the task status on the console every 5 seconds (`/api/get_task_progress`).
   - When the PS4 has fully downloaded the file and reported no error, the local `.pkg` is deleted.
   - Only local `.pkg` files sent from the "file" tab are deleted. Internet links and the cache are never touched.
   - If the task fails, the console stops responding, or 24 hours pass, the file is kept.
   - Actions are written to `DPI-CLEANUP.log` next to the settings.
   - Does not work with GoldHEN Payload Server or etaHEN, because they have no task status.
   - Deletion happens when the console has **finished downloading**, not necessarily when the game is fully installed.

2. **Torrents: magnet link, `.torrent`, or a URL to a `.torrent`**
   - Paste a magnet link or a `.torrent` path into the link field (or pick the file with Open, or drag it into the window on desktop) and press **Load**.
   - Only `.pkg` files from the torrent are downloaded; everything else is skipped. The status shows percent, speed and peer count.
   - If the torrent contains several `.pkg` files, they appear in the **Packages** menu.
   - After the download, the file opens like any local file and the usual flow continues (including auto-delete).
   - Files are stored in the `Torrents` folder next to the settings.
   - Uses the [MonoTorrent](https://github.com/alanmcgovern/monotorrent) library, version 3.0.2 (MIT license).

### Limitations
- The whole `.pkg` is downloaded to the device before it is loaded into the app, so free disk space is required.
- With auto-delete enabled, the torrent stops seeding.
- The Android build is not fully verified: there may be startup problems, and the cause is under investigation. Crash logs: `DPI-CRASH.log` and `DPI-AndroidCrash.log` in the app's data folder.

### Usage and legality
The app is intended for installing packages you are entitled to use (your own builds, homebrew, backups of games you own). The user is responsible for the content they use.

### License and credits
GPL-3.0, same as the original. Upstream project: marcussacana/DirectPackageInstaller. Console receiver: Remote Package Installer (flatz) and its OpenOrbis SDK port.

---

## Русский

**RayBPS4** это форк [DirectPackageInstaller](https://github.com/marcussacana/DirectPackageInstaller) (автор оригинала: marcussacana, лицензия GPL-3.0) с двумя дополнительными функциями: **автоудаление `.pkg` после передачи на PS4** и **загрузка через торрент (magnet-ссылка или `.torrent`)**.

> Обе новые функции экспериментальные. Код написан без возможности запуска на реальной консоли, поэтому проверяйте их на своём устройстве.

### Что умеет программа

**Из оригинала**
- Отправляет `.pkg` на PS4 с компьютера или телефона по прямой ссылке или из локального файла, не скачивая его заранее на консоль вручную.
- Работает через приёмник на консоли: **Remote Package Installer** или **GoldHEN Payload Server**.
- Принимает ссылки через сервисы, настраиваемые на вкладке **Debrids** (AllDebrid, RealDebrid, DebridLink), а также прямые ссылки и Google Drive.
- Платформы: Windows, Linux, macOS, Android.

**Добавлено в этом форке**

1. **Delete PKG after transfer** (меню Options)
   - После отправки файла через Remote Package Installer программа раз в 5 секунд опрашивает статус задачи на консоли (`/api/get_task_progress`).
   - Когда PS4 полностью скачала файл и ошибок нет, локальный `.pkg` удаляется.
   - Удаляются только локальные файлы `.pkg`, отправленные из вкладки «файл». Ссылки из интернета и кэш не затрагиваются.
   - Если задача завершилась ошибкой, консоль не отвечает или прошло 24 часа, файл остаётся.
   - Действия записываются в `DPI-CLEANUP.log` рядом с настройками.
   - Не работает с GoldHEN Payload Server и etaHEN: у них нет статуса задачи.
   - Удаление срабатывает, когда консоль **закончила скачивание**, а не обязательно когда игра полностью установилась.

2. **Торренты: magnet-ссылка, `.torrent` или ссылка на `.torrent`**
   - Вставьте magnet или путь к `.torrent` в поле ссылки (или выберите файл через Open, или перетащите его в окно на ПК) и нажмите **Load**.
   - Из раздачи скачиваются только файлы `.pkg`, остальное пропускается. Статус показывает проценты, скорость и число пиров.
   - Если в раздаче несколько `.pkg`, они появляются в меню **Packages**.
   - После скачивания файл открывается как обычный локальный, дальше всё как обычно (в том числе автоудаление).
   - Файлы хранятся в папке `Torrents` рядом с настройками.
   - Используется библиотека [MonoTorrent](https://github.com/alanmcgovern/monotorrent) 3.0.2 (лицензия MIT).

### Ограничения
- Весь `.pkg` скачивается на устройство целиком до загрузки в программу, поэтому нужно свободное место.
- При включённом автоудалении торрент перестаёт раздаваться.
- Android-сборка проверена не полностью: возможны проблемы при запуске, причина выясняется. Журналы падений: `DPI-CRASH.log` и `DPI-AndroidCrash.log` в папке данных приложения.

### Использование и законность
Программа предназначена для установки пакетов, которыми вы вправе пользоваться (своих сборок, homebrew, резервных копий собственных игр). Ответственность за используемый контент лежит на пользователе.

### Лицензия и благодарности
GPL-3.0, как и оригинал. Исходный проект: marcussacana/DirectPackageInstaller. Консольный приёмник: Remote Package Installer (flatz) и его порт на OpenOrbis SDK.

# Выпуск LogBook ПК

1. Обновить Version/FileVersion в logbook.csproj и добавить описание версии в release-notes.json.
2. Проверить изменённую функциональность и собрать Release Publish. Target CreateReleaseManifest автоматически создаёт version.json рядом с EXE и releases/<номер>.json. Номер, SHA-256 и размер берутся из готового EXE.
3. Если готовая сборка размещена для скачивания, передать MSBuild свойство ReleaseDownloadUrl с проверенной HTTPS-ссылкой на эту сборку. Без ссылки JSON содержит пустой url, и приложение скрывает кнопку скачивания. Не указывать ссылку на прежний EXE.
4. После успешной сборки загрузить JSON в существующий Supabase Storage bucket updates: архив releases/<номер>.json, затем текущий version.json. Проверить публичное чтение и номер версии.
5. При доступе через Supabase Dashboard требуется вход пользователя; ключи service_role и пароли не помещать в проект, EXE или version.json. Подключённый SQL-инструмент не загружает содержимое объектов Storage; не изменять storage.objects вручную.

Публичный адрес текущего JSON: https://zjagjauwmpuzcwzidhkn.supabase.co/storage/v1/object/public/updates/version.json

Разрешение пользователя: при создании новой версии сохранять JSON о выпуске на Supabase. Обновлять после готовой и проверенной сборки. До подтверждения публичного чтения не считать публикацию завершённой.

# Learner+Rabbit
 Строка подлючения rabbit в файлах RabbitConnection.json для client и server в bin
Чистые веса для старта обучения: https://pjreddie.com/media/files/darknet53.conv.74, но можно использовать и сови

Процесс работы:
1) Прописать строку подключения в RabbitConnection.json для client и server
2) Запустить client и server
3) На client через вкладку меню "Файл" выбрать датасет (.data), файл конфигурации (.cfg), веса для обучения (.weights)
4) Запустить обучение
5) Дождаться уведомлений в Log о создании файлов весов и остановить обучение

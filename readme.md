> [!WARNING]
> Проект остановлен. При изучении вопроса с юридической стороны вскрылась проблема. Если позволить физлицам зарабатывать на твоём ресурсе, ты будешь знать об этом, но не заявишь в налоговую, а само физлицо не заплатит НДФЛ, то ты становишься пособником по ведению нелегального бизнеса. Чтобы это обойти нужно либо заставить всех стать юрлицами (рассматривать не вижу смысла, так как БлаБлаКар'ом пользуются как раз потому что можно заработать не платя налог), либо стать копией ББК с ограничениями стоимости, без плюшек водителям типа регулярных поездок. Второй вариант хоть и имеет право на жизнь, но непонятно, зачем тогда люди уйдут с ББК на этот сервис. Либо ещё третий вариант - нарушить закон, продаться кому-нибудь, и тем самым перенести все проблемы на нового владельца. Ни один из вариантов мне не нравится. Поэтому проект прекращает свой путь.



# Полезные команды:

## Запуск 3rd party

```bash
cd ./scripts/dev

BBC_REDIS_DATA_PATH=/c/bbc/redis_not_default_path BBC_SEQ_DATA_PATH=/c/bbc/seq_not_default_path docker-compose -f 3rd-party-docker-compose.yaml up -d
```

- `BBC_REDIS_DATA_PATH` задаёт путь для дампов redis
- `BBC_SEQ_DATA_PATH` задаёт путь для данных seq

## Миграции EF

> [!WARNING]
> В определённый момент от EF было принято решение отказаться. Почти наверняка эта команда уже ничего не даст

```bash
cd ./src/WebApi/

dotnet ef migrations add NAME
```

# Полезные ссылки

- http://localhost:32770/swagger/index.html
- http://localhost:32770/rin/
- http://localhost:81/ (Это seq. Для неё неохота было урл прописывать)

# Рекомендуемые vs code расширения

```bash
code --install-extension cweijan.dbclient-jdbc
code --install-extension cweijan.vscode-redis-client
code --install-extension dotjoshjohnson.xml
code --install-extension eamodio.gitlens
code --install-extension icsharpcode.ilspy-vscode
code --install-extension jmrog.vscode-nuget-package-manager
code --install-extension ms-azuretools.vscode-docker
code --install-extension ms-dotnettools.csdevkit
code --install-extension ms-dotnettools.csharp
code --install-extension ms-dotnettools.vscode-dotnet-runtime
code --install-extension ms-dotnettools.vscodeintellicode-csharp
code --install-extension streetsidesoftware.code-spell-checker
code --install-extension streetsidesoftware.code-spell-checker-russian
```

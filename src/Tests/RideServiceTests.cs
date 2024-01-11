using AutoFixture;

using CsvHelper;
using CsvHelper.Configuration;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

using Serilog;

using System.Globalization;

using WebApi.DataAccess;
using WebApi.Models;
using WebApi.Services.Core;
using WebApi.Services.Yandex;

namespace Tests
{
	public class RideServiceTests : IClassFixture<TestAppFactoryWithDb>
	{
		private readonly IServiceProvider _provider;
		private readonly Fixture _fixture;
		private readonly IRideService _rideService;

		public RideServiceTests(TestAppFactoryWithDb factory)
		{
			_provider = factory.Services;
			factory.MigrateDb();
			_fixture = Shared.BuildDefaultFixture();
			_rideService = _provider.GetRequiredService<IRideService>();
		}

		[Fact]
		public async Task CreateRideCorrectFullyLeg()
		{
			using var scope = _provider.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

			var ride = _fixture.Create<RideDto>();
			var legsCount = ValidLegWaypointCounts(5).Last().legsCount;
			var legs = _fixture
				.Build<LegDto>()
				.With(x => x.Ride, ride)
				.With(x => x.RideId, ride.Id)
				.CreateMany(legsCount)
				.ToArray();
			ride.Legs = legs;
			NormalizeFromTo(legs);

			var result = await _rideService.CreateRide(context, ride, CancellationToken.None);

			result.FullyLeg.Should().Be(result.Legs!.OrderByDescending(x => x.Duration).First());
		}

		[Fact]
		public void TestFormatOfPoint()
		{
			var point = _fixture.Create<PlaceAndTime>();

			var pointAsJson = JsonConvert.SerializeObject(point);
			var pointAsStr = $"{point}";

			var pointFromJson = JsonConvert.DeserializeObject<PlaceAndTime>(pointAsJson);
			var pointFromStr = JsonConvert.DeserializeObject<PlaceAndTime>(pointAsStr);

			pointAsStr.Should().Be(pointAsJson);

			pointFromStr.Should().BeEquivalentTo(point);
			pointFromStr.Should().BeEquivalentTo(pointFromJson);

			pointFromJson.Should().BeEquivalentTo(point);
			pointFromJson.Should().BeEquivalentTo(pointFromStr);
		}

		[Fact]
		public async Task CollectCityInfos()
		{
			throw new NotSupportedException();

			var all = @"Севастополь
Москва
Санкт-Петербург
Новосибирск
Екатеринбург
Казань
Нижний Новгород
Челябинск
Красноярск
Самара
Уфа
Ростов-на-Дону
Омск
Краснодар
Воронеж
Пермь
Волгоград
Саратов
Тюмень
Тольятти
Барнаул
Ижевск
Махачкала
Хабаровск
Ульяновск
Иркутск
Владивосток
Ярославль
Кемерово
Томск
Набережные Челны
Ставрополь
Оренбург
Новокузнецк
Рязань
Балашиха
Пенза
Чебоксары
Липецк
Калининград
Астрахань
Тула
Киров
Сочи
Курск
Улан-Удэ
Тверь
Магнитогорск
Сургут
Брянск
Иваново
Якутск
Владимир
Симферопольне призн.
Белгород
Нижний Тагил
Калуга
Чита
Грозный
Волжский
Смоленск
Подольск
Саранск
Вологда
Курган
Череповец
Орёл
Архангельск
Владикавказ
Нижневартовск
Йошкар-Ола
Стерлитамак
Мурманск
Кострома
Новороссийск
Тамбов
Химки
Мытищи
Нальчик
Таганрог
Нижнекамск
Благовещенск
Комсомольск-на-Амуре
Петрозаводск
Королёв
Шахты
Энгельс
Великий Новгород
Люберцы
Братск
Старый Оскол
Ангарск
Сыктывкар
Дзержинск
Псков
Орск
Красногорск
Армавир
Абакан
Балаково
Бийск
Южно-Сахалинск
Одинцово
Уссурийск
Прокопьевск
Рыбинск
Норильск
Волгодонск
Сызрань
Петропавловск-Камчатский
Каменск-Уральский
Новочеркасск
Альметьевск
Златоуст
Северодвинск
Хасавюрт
Керчьне призн.
Домодедово
Салават
Миасс
Копейск
Пятигорск
Электросталь
Майкоп
Находка
Березники
Коломна
Щёлково
Серпухов
Ковров
Нефтекамск
Кисловодск
Батайск
Рубцовск
Обнинск
Кызыл
Дербент
Нефтеюганск
Назрань
Каспийск
Долгопрудный
Новочебоксарск
Новомосковск
Ессентуки
Невинномысск
Октябрьский
Раменское
Первоуральск
Михайловск
Реутов
Черкесск
Жуковский
Димитровград
Пушкино
Артём
Камышин
Евпаторияне призн.
Муром
Ханты-Мансийск
Новый Уренгой
Северск
Орехово-Зуево
Арзамас
Ногинск
Новошахтинск
Бердск
Элиста
Сергиев Посад
Видное
Ачинск
Тобольск
Ноябрьск
Елец
Зеленодольск
Новокуйбышевск
Воткинск
Железногорск
Междуреченск
Воскресенск
Гатчина
Серов
Саров
Ленинск-Кузнецкий
Сарапул
Магадан
Мичуринск
Соликамск
Мурино
Чехов
Клин
Бузулук
Глазов
Канск
Великие Луки
Каменск-Шахтинский
Губкин
Киселёвск
Ейск
Ивантеевка
Лобня
Железногорск
Азов
Анапа
Бугульма
Геленджик
Ухта
Юрга
Усть-Илимск
Всеволожск
Новоуральск
Кузнецк
Бор
Кинешма
Озёрск
Новотроицк
Кропоткин
Чайковский
Черногорск
Усолье-Сибирское
Ялтане призн.
Дубна
Балашов
Елабуга
Новоалтайск
Выборг
Егорьевск
Верхняя Пышма
Наро-Фоминск
Минеральные Воды
Троицк
Чапаевск
Минусинск
Биробиджан
Шадринск
Белово
Туймазы
Сертолово
Буйнакск
Ишим
Кирово-Чепецк
Анжеро-Судженск
Феодосияне призн.
Дмитров
Сосновый Бор
Горно-Алтайск
Лыткарино
Павловский Посад
Белорецк
Ступино
Гудермес
Ишимбай
Донской
Котельники
Кстово
Урус-Мартан
Георгиевск
Клинцы
Нягань
Славянск-на-Кубани
Кунгур
Сунжа
Туапсе
Когалым
Белогорск
Лениногорск
Россошь
Алексин
Кудрово
Борисоглебск
Фрязино
Гуково
Ревда
Прохладный
Берёзовский
Белебей
Чистополь
Заречный
Будённовск
Кумертау
Сальск
Дзержинский
Лабинск
Асбест
Искитим
Павлово
Александров
Воркута
Сибай
Мелеуз
Котлас
Михайловка
Избербаш
Краснотурьинск
Белореченск
Ржев
Лесосибирск
Тихорецк
Тихвин
Шуя
Полевской
Щёкино
Шали
Вольск
Крымск
Зеленогорск
Лиски
Черемхово
Лысьва
Нерюнгри
Волжск
Мегион
Вязьма
Тимашёвск
Гусь-Хрустальный
Краснокаменск
Кириши
Снежинск
Жигулёвск
Кизляр
Кингисепп
Апатиты
Узловая
Краснокамск
Балахна
Свободный
Солнечногорск
Аксай
Лесной
Арсеньев
Салехард
Боровичи
Рассказово
Курганинск
Отрадный
Донецк
Надым
Кашира
Вышний Волочёк
Чусовой
Рославль
Назарово
Выкса
Берёзовский
Саяногорск
Чебаркуль
Канаш
Можга
Бирск
Грязи
Краснознаменск
Бугуруслан
Радужный
Ливны
Североморск
Карабулак
Рузаевка
Лангепас
Сатка
Шелехов
Куйбышев
Малоярославец
Кореновск
Большой Камень
Аргун
Темрюк
Ярцево
Урай
Заринск
Торжок
Верхняя Салда
Лянтор
Горячий Ключ
Кимры
Мариинск
Белая Калитва
Сосновоборск
Осинники
Курчатов
Апшеронск
Пыть-Ях
Усть-Лабинск
Пугачёв
Мыски
Мончегорск
Заинск
Шебекино
Тутаев
Баксан
Абинск
Кольчугино
Стрежевой
Моршанск
Советск
Ялуторовск
Новозыбков
Изобильный
Амурск
Волхов
Тулун
Луга
Сафоново
Кизилюрт
Югорск
Шатура
Ртищево
Переславль-Залесский
Протвино
Южноуральск
Истра
Качканар
Красноуфимск
Коркино
Джанкойне призн.
Ирбит
Мценск
Усть-Кут
Моздок
Заволжье
Кинель
Урюпинск
Реж
Алексеевка
Ефремов
Малгобек
Елизово
Вязники
Алапаевск
Учалы
Черняховск
Кыштым
Беслан
Людиново
Звенигород
Спасск-Дальний
Светлоград
Красный Сулин
Фролово
Ахтубинск
Саянск
Апрелевка
Благовещенск
Лесозаводск
Печора
Богородск
Миллерово
Азнакаево
Сокол
Сланцы
Коряжма
Тайшет
Ликино-Дулёво
Тосно
Мирный
Новокубанск
Нурлат
Шарыпово
Корсаков
Можайск
Партизанск
Дальнегорск
Конаково
Каменка
Гулькевичи
Новодвинск
Гай
Губкинский
Нарткала
Зеленокумск
Валуйки
Чернушка
Тавда
Сухой Лог
Углич
Трёхгорный
Камень-на-Оби
Алатырь
Кулебаки
Усинск
Острогожск
Дагестанские Огни
Алуштане призн.
Тейково
Дюртюли
Советский
Усть-Джегута
Приморско-Ахтарск
Кохма
Благодарный
Ачхой-Мартан
Дедовск
Вичуга
Нововоронеж
Зима
Обь
Сердобск
Богданович
Нижнеудинск
Электрогорск
Луховицы
Вятские Поляны
Фурманов
Борзя
Богородицк
Гусев
Муравленко
Слободской
Кандалакша
Балабаново
Лосино-Петровский
Артёмовский
Добрянка
Маркс
Великий Устюг
Городец
Тында
Бахчисарайне призн.
Сорочинск
Касимов
Кудымкар
Ростов
Заречный
Киров
Семилуки
Славгород
Аша
Барабинск
Еманжелинск
Старая Русса
Дивногорск
Похвистнево
Киржач
Кушва
Мирный
Кировск
Топки
Камышлов
Карталы
Заводоуковск
Тара
Шумерля
Балтийск
Новоалександровск
Гурьевск
Котовск
Майский
Кувандык
Гагарин
Красноармейск
Кимовск
Волоколамск
Петровск
Соль-Илецк
Ипатово
Костомукша
Удомля
Янаул
Карпинск
Кондопога
Коммунар
Отрадное
Холмск
Полысаево
Красноперекопскне призн.
Киреевск
Лабытнанги
Алейск
Десногорск
Дятьково
Скопин
Семёнов
Асино
Карасук
Кировск
Знаменск
Гусиноозёрск
Североуральск
Бутурлиновка
Озёры
Сакине призн.
Калач-на-Дону
Унеча
Морозовск
Северобайкальск
Советская Гавань
Родники
Зерноград
Карачаевск
Строитель
Татарск
Медногорск
Дальнереченск
Уварово
Сегежа
Курчалой
Нарьян-Мар
Губаха
Среднеуральск
Кубинка
Нефтекумск
Верхний Уфалей
Старая Купавна
Менделеевск
Железноводск
Голицыно
Аткарск
Лермонтов
Павловск
Тайга
Никольское
Верещагино
Сосногорск
Гурьевск
Хадыженск
Невьянск
Тырныауз
Котельниково
Таштагол
Давлеканово
Бронницы
Вилючинск
Калтан
Вихоревка
Семикаракорск
Лысково
Бавлы
Сасово
Железногорск-Илимский
Вельск
Алдан
Алагир
Красноуральск
Бежецк
Усть-Катав
Оленегорск
Рошаль
Ленск
Калачинск
Красноармейск
Светлый
Рыбное
Котово
Остров
Бобров
Колпашево
Новопавловск
Тогучин
Зарайск
Чегем
Октябрьск
Армянскне призн.
Ряжск
Сысерть
Буй
Исилькуль
Хотьково
Шарья
Арск
Нижний Ломов
Пикалёво
Оха
Инта
Сергач
Бологое
Котельнич
Лебедянь
Белоярский
Агрыз
Нерехта
Буинск
Терек
Тарко-Сале
Черепаново
Никольск
Куровское
Ковылкино
Козьмодемьянск
Данков
Фокино
Усмань
Омутнинск
Пущино
Дудинка
Черноголовка
Оса
Зея
Зверево
Арамиль
Пролетарск
Ардон
Лодейное Поле
Приозерск
Кировград
Николаевск-на-Амуре
Нелидово
Харабали
Няндома
Нижняя Тура
Пласт
Новый Оскол
Суровикино
Боготол
Ершов
Нефтегорск
Слюдянка
Электроугли
Кукмор
Кяхта
Судакне призн.
Баймак
Покров
Стародуб
Жуковка
Шахунья
Калач
Суворов
Радужный
Льгов
Енисейск
Карачев
Белогорскне призн.
Собинка
Талдом
Юрьев-Польский
Абдулино
Константиновск
Куса
Онега
Новомичуринск
Плавск
Козельск
Нытва
Осташков
Зеленоградск
Туринск
Краснослободск
Нижняя Салда
Шимановск
Яровое
Поворино
Бакал
Инза
Шумиха
Бикин
Жуков
Светлогорск
Бокситогорск
Кирсанов
Камызяк
Подпорожье
Гаврилов-Ям
Покачи
Поронайск
Руза
Мензелинск
Иланский
Сельцо
Райчихинск
Ковдор
Кондрово
Мамадыш
Межгорье
Болотное
Кизел
Жирновск
Дегтярск
Свирск
Ясный
Касли
Новоаннинский
Нерчинск
Магас
Ясногорск
Новоузенск
Бородино
Рыльск
Купино
Петровск-Забайкальский
Почеп
Палласовка
Калининск
Щигры
Барыш
Сортавала
Талица
Куртамыш
Сухиничи
Заполярный
Дубовка
Белокуриха
Цимлянск
Навашино
Катав-Ивановск
Краснозаводск
Советск
Грязовец
Красновишерск
Очёр
Богучар
Волгореченск
Приволжск
Ивдель
Чудово
Красный Кут
Яранск
Агидель
Полярные Зори
Ужур
Шлиссельбург
Гвардейск
Кашин
Валдай
Колтуши
Пестово
Яхрома
Невель
Жердевка
Лагань
Светогорск
Белоозёрский
Данилов
Новоульяновск
Николаевск
Меленки
Ленинск
Кодинск
Петушки
Трубчевск
Первомайск
Анадырь
Байкальск
Адыгейск
Карабаново
Высоковск
Мантурово
Южа
Удачный
Лакинск
Сим
Галич
Белёв
Пионерский
Вяземский
Цивильск
Венёв
Гороховец
Лукоянов
Калязин
Боровск
Сясьстрой
Фокино
Петров Вал
Ак-Довурак
Урень
Могоча
Дрезна
Полярный
Абаза
Шилка
Хвалынск
Уяр
Камешково
Покровск
Медвежьегорск
Волосово
Катайск
Обоянь
Струнино
Шагонар
Пересвет
Кремёнки
Долинск
Бабаево
Далматово
Чаплыгин
Чкаловск
Торопец
Сосенский
Закаменск
Сураж
Нариманов
Горнозаводск
Чулым
Лихославль
Киренск
Емва
Звенигово
Аркадак
Белоусово
Ермолино
Александровск
Новая Ляля
Невельск
Заозёрный
Тетюши
Карабаш
Южно-Сухокумск
Старый Крымне призн.
Называевск
Судогда
Вытегра
Нязепетровск
Кораблино
Михайлов
Балей
Юрюзань
Печоры
Ворсма
Нюрба
Щёлкиноне призн.
Сорск
Верхний Тагил
Горняк
Камбарка
Вилюйск
Эртиль
Кемь
Малая Вишера
Окуловка
Хилок
Снежногорск
Опочка
Дигора
Таруса
Тюкалинск
Задонск
Ивангород
Зуевка
Михайловск
Чадан
Володарск
Белая Холуница
Анива
Завитинск
Дорогобуж
Болхов
Змеиногорск
Болохово
Вуктыл
Гаджиево
Суздаль
Кувшиново
Неман
Луза
Лаишево
Алупкане призн.
Теберда
Кола
Перевоз
Кирс
Верхнеуральск
Бодайбо
Краснослободск
Рудня
Заволжск
Ардатов
Александровск-Сахалинский
Каргополь
Тотьма
Белинский
Серафимович
Бирюсинск
Волчанск
Верхняя Тура
Микунь
Петухово
Миньяр
Сольцы
Уржум
Сосновка
Олёкминск
Харовск
Белозерск
Комсомольск
Гремячинск
Питкяранта
Ельня
Липки
Каргат
Мамоново
Болгар
Городовиковск
Нолинск
Щучье
Медынь
Наволоки
Углегорск
Нижние Серги
Облучье
Инсар
Ядрин
Юрьевец
Советск
Западная Двина
Мариинский Посад
Дно
Устюжна
Нея
Городище
Козловка
Заозёрск
Беломорск
Ветлуга
Олонец
Никольск
Починок
Сычёвка
Новая Ладога
Пудож
Порхов
Циолковский
Суоярви
Каменногорск
Кириллов
Бирюч
Костерёво
Салаир
Сковородино
Андреаполь
Старица
Спасск
Малмыж
Полесск
Мглин
Новосокольники
Пучеж
Макушино
Севск
Верхотурье
Усолье
Юхнов
Приморск
Темников
Княгинино
Томмот
Оханск
Багратионовск
Сенгилей
Весьегонск
Демидов
Дмитриев
Курлово
Велиж
Себеж
Зубцов
Грайворон
Сретенск
Сурск
Лахденпохья
Новохопёрск
Шацк
Макаров
Спасск-Рязанский
Мураши
Короча
Мышкин
Красавино
Билибино
Жиздра
Солигалич
Макарьев
Орлов
Гаврилов Посад
Алзамай
Злынка
Пыталово
Дмитровск
Шиханы
Пошехонье
Суджа
Туран
Любим
Красный Холм
Верея
Спас-Клепики
Фатеж
Шенкурск
Чердынь
Спас-Деменск
Любань
Сусуман
Бабушкин
Томари
Чухлома
Мосальск
Славск
Озёрск
Кадников
Пустошка
Певек
Духовщина
Правдинск
Иннополис
Мещовск
Ладушкин
Игарка
Малоархангельск
Гдов
Краснознаменск
Нестеров
Новоржев
Холм
Среднеколымск
Белый
Чёрмоз
Новосиль
Мезень
Курильск
Кологрив
Северо-Курильск
Горбатов
Сольвычегодск
Плёс
Кедровый
Артёмовск
Островной
Приморск
Высоцк
Чекалин
Верхоянск";

			var cityNames = all.Split("\r\n", options: StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.ToArray();

			var cts = new CancellationTokenSource();

			using var scope = _provider.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
			var suggestService = scope.ServiceProvider.GetRequiredService<ISuggestService>();
			var geocodeService = scope.ServiceProvider.GetRequiredService<IGeocodeService>();

			var suggestionTasks = cityNames
				.Take(980)
				.Select(x => suggestService.GetSuggestion(x, cts.Token).AsTask())
				.ToArray();
			var suggestions = await Task.WhenAll(suggestionTasks);

			foreach (var suggestion in suggestions)
			{
				Log.Information("{Suggestion}", JsonConvert.SerializeObject(suggestion));
			}

			File.WriteAllText("C:/temp/citySuggestions.txt", JsonConvert.SerializeObject(suggestions));

			var geocodeTasks = suggestions
				.Select(x => geocodeService.UriToGeoCode(x.Results[0].Uri, cts.Token).AsTask())
				.ToArray();

			var geocodes = await Task.WhenAll(geocodeTasks);

			foreach (var geocode in geocodes)
				Log.Information("{Geocode}", geocode);

			File.WriteAllText("C:/temp/cityGeocodes.txt", JsonConvert.SerializeObject(geocodes));
		}

		[Fact]
		public async Task CollectCityInfos_v2()
		{
			throw new NotSupportedException();

			var cts = new CancellationTokenSource();

			using var scope = _provider.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
			var suggestService = scope.ServiceProvider.GetRequiredService<ISuggestService>();
			var geocodeService = scope.ServiceProvider.GetRequiredService<IGeocodeService>();

			var suggestionsJson = File.ReadAllText("C:/temp/citySuggestions.txt");
			var suggestions = JsonConvert.DeserializeObject<YandexSuggestResponseDto[]>(suggestionsJson);

			var geocodeTasks = suggestions
				.Where(x => x?.Results?.Count > 0 && !string.IsNullOrWhiteSpace(x?.Results?.First().Uri))
				.Select(x => geocodeService.UriToGeoCode(x.Results[0].Uri, cts.Token).AsTask())
				.ToArray();

			var geocodes = await Task.WhenAll(geocodeTasks);

			foreach (var geocode in geocodes)
				Log.Information("{Geocode}", geocode);

			File.WriteAllText("C:/temp/cityGeocodes.txt", JsonConvert.SerializeObject(geocodes));
		}

		[Fact]
		public void CreateCsv()
		{
			var suggestions = JsonConvert.DeserializeObject<YandexSuggestResponseDto[]>(File.ReadAllText("C:/temp/citySuggestions.txt"))!
				.Where(x => x?.Results?.Count > 0 && !string.IsNullOrWhiteSpace(x?.Results?.First().Uri))
				.ToArray();
			var geocodes = JsonConvert.DeserializeObject<YandexGeocodeResponseDto[]>(File.ReadAllText("C:/temp/cityGeocodes.txt"))!;

			var zip = new List<Dummy>(suggestions.Length);
			for (int i = 0; i < suggestions.Length; i++)
			{
				zip.Add(new Dummy() { Suggest = suggestions[i], Geocode = geocodes[i] });
			}

			var csvRows = zip.Select(x => new
			{
				x.Suggest.Results[0].FormattedAddress,
				x.Suggest.Results[0].Title,
				x.Suggest.Results[0].Uri,
				x.Geocode.Geoobjects[0].Point.Latitude,
				x.Geocode.Geoobjects[0].Point.Longitude,
			}).ToArray();


			var conf = new CsvConfiguration(CultureInfo.InvariantCulture);
			conf.Delimiter = ",";


			using (var writer = new StreamWriter("C:/temp/cityInfos.csv"))
			using (var csv = new CsvWriter(writer, conf))
			{
				csv.WriteRecords(csvRows);
			}
		}

		public class Dummy
		{
			public YandexSuggestResponseDto Suggest;
			public YandexGeocodeResponseDto Geocode;
		}

		[Fact]
		public async Task TestCsv()
		{
			var conf = new CsvConfiguration(CultureInfo.InvariantCulture);
			conf.Delimiter = ",";
			conf.NewLine = "\r\n";

			List<CityInfo> cities;

			using (var reader = new StreamReader("./CityInfos.csv"))
			using (var csv = new CsvReader(reader, conf))
			{
				cities = csv.GetRecords<CityInfo>().ToList();
			}

			using var scope = _provider.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
			var suggestService = scope.ServiceProvider.GetRequiredService<ISuggestService>();
			var geocodeService = scope.ServiceProvider.GetRequiredService<IGeocodeService>();

			var lol = await geocodeService.PointToGeoCode(new FormattedPoint() { Latitude = cities[387].Latitude, Longitude = cities[387].Longitude }, CancellationToken.None);

			Console.WriteLine(lol.ToString());
		}


		private static IEnumerable<(int legsCount, int waypointsCount)> ValidLegWaypointCounts(int maxWaypointsCount)
		{
			var lastValidLegCount = 1;
			yield return (lastValidLegCount, 2);

			for (int waypointCount = 3; waypointCount <= maxWaypointsCount; waypointCount++)
			{
				lastValidLegCount += waypointCount - 1;
				yield return (lastValidLegCount, waypointCount);
			}
		}

		private void NormalizeFromTo(IReadOnlyList<LegDto> legs)
		{
			var waypointsCount = ValidLegWaypointCounts(10)
				.First(x => x.legsCount == legs.Count)
				.waypointsCount;

			var points = _fixture.CreateMany<PlaceAndTime>(waypointsCount).ToArray();

			var now = DateTimeOffset.UtcNow;
			for (int i = 0; i < points.Length; i++)
				points[i].DateTime = now.AddHours(i);

			var allPairs = GetAllPairs(points).ToArray();

			allPairs.Should().HaveSameCount(legs);

			for (int i = 0; i < allPairs.Length; i++)
			{
				legs[i].From = allPairs[i].from;
				legs[i].To = allPairs[i].to;
			}
		}

		private IEnumerable<(PlaceAndTime from, PlaceAndTime to)> GetAllPairs(IReadOnlyList<PlaceAndTime> points)
		{
			for (int i = 0; i < points.Count; i++)
			{
				for (int j = i + 1; j < points.Count; j++)
				{
					yield return (points[i], points[j]);
				}
			}
		}
	}
}

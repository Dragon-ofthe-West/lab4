// Базовый декоратор
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

public abstract class EnemyDecorator : Enemy
{
    protected readonly Enemy _wrappedEnemy;
    protected readonly GameLogger _logger;

    public EnemyDecorator(Enemy enemy)
    {
        _wrappedEnemy = enemy;
        _logger = GameLogger.Instance;
    }

    public override string Name => _wrappedEnemy.Name;
    public override int Health => _wrappedEnemy.Health;
    public override int Damage => _wrappedEnemy.Damage;

    public override void TakeDamage(int damage)
    {
        _wrappedEnemy.TakeDamage(damage);
    }

    public override void Attack(PlayableCharacter player)
    {
        _wrappedEnemy.Attack(player);
    }

    public override bool IsAlive => _wrappedEnemy.IsAlive;
}

// Легендарный модификатор - увеличивает урон
public class LegendaryEnemyDecorator : EnemyDecorator
{
    private const int AdditionalDamage = 20;

    public LegendaryEnemyDecorator(Enemy enemy) : base(enemy) { }

    public override string Name => $"Легендарный {_wrappedEnemy.Name}";

    public override void Attack(PlayableCharacter player)
    {
        base.Attack(player);
        _logger.Log("Враг легендарный и наносит дополнительный урон!!!");
        player.TakeDamage(AdditionalDamage);
    }
}

// Модификатор неистовства ветра - атакует дважды
public class WindfuryEnemyDecorator : EnemyDecorator
{
    public WindfuryEnemyDecorator(Enemy enemy) : base(enemy) { }

    public override string Name => $"Обладающий Неистовством Ветра {_wrappedEnemy.Name}";

    public override void Attack(PlayableCharacter player)
    {
        base.Attack(player);
        _logger.Log("Неистовство ветра позволяет врагу атаковать второй раз!!!");
        base.Attack(player); // Вторая атака
    }
}

// ДОПОЛНИТЕЛЬНО: Модификатор брони - уменьшает получаемый урон
public class ArmoredEnemyDecorator : EnemyDecorator
{
    private readonly float _damageReduction;

    public ArmoredEnemyDecorator(Enemy enemy, float damageReduction = 0.3f) : base(enemy)
    {
        _damageReduction = damageReduction;
    }

    public override string Name => $"Бронированный {_wrappedEnemy.Name}";

    public override void TakeDamage(int damage)
    {
        int reducedDamage = (int)(damage * (1 - _damageReduction));
        _logger.Log($"Броня поглощает {_damageReduction * 100}% урона!");
        base.TakeDamage(reducedDamage);
    }
}




// Модель профиля игрока
[Serializable]
public class PlayerProfile
{
    public string Name { get; set; }
    public int Score { get; set; }

    public PlayerProfile(string name, int score)
    {
        Name = name;
        Score = score;
    }
}

// Интерфейс репозитория
public interface IPlayerProfileRepository
{
    PlayerProfile GetProfile(string name);
    void UpdateHighScore(string name, int score);
}

// Реальная реализация (работа с файлом)
public class PlayerProfileDBRepository : IPlayerProfileRepository
{
    private const string ScoreFileName = "score.dat";

    public PlayerProfileDBRepository()
    {
        if (!File.Exists(ScoreFileName))
        {
            SaveProfiles(new Dictionary<string, PlayerProfile>());
        }
    }

    public PlayerProfile GetProfile(string name)
    {
        Console.WriteLine("Из базы данных достается информация о профилях игроков..");
        var profiles = LoadProfiles();

        if (!profiles.ContainsKey(name))
        {
            Console.WriteLine("В базе данных создается новый профиль...");
            profiles[name] = new PlayerProfile(name, 0);
            SaveProfiles(profiles);
        }

        return profiles[name];
    }

    public void UpdateHighScore(string name, int score)
    {
        Console.WriteLine("В базе данных обновляются очки игрока...");
        var profiles = LoadProfiles();

        if (!profiles.ContainsKey(name))
        {
            Console.WriteLine("В базе данных создается новый профиль...");
            profiles[name] = new PlayerProfile(name, 0);
        }

        profiles[name].Score = score;
        SaveProfiles(profiles);
    }

    private Dictionary<string, PlayerProfile> LoadProfiles()
    {
        try
        {
            using var stream = File.OpenRead(ScoreFileName);
            var formatter = new BinaryFormatter();
            return (Dictionary<string, PlayerProfile>)formatter.Deserialize(stream);
        }
        catch
        {
            return new Dictionary<string, PlayerProfile>();
        }
    }

    private void SaveProfiles(Dictionary<string, PlayerProfile> profiles)
    {
        using var stream = File.Create(ScoreFileName);
        var formatter = new BinaryFormatter();
        formatter.Serialize(stream, profiles);
    }
}

// Кеширующий прокси
public class PlayerProfileCacheRepository : IPlayerProfileRepository
{
    private readonly Dictionary<string, PlayerProfile> _cachedProfiles;
    private readonly PlayerProfileDBRepository _database;

    public PlayerProfileCacheRepository()
    {
        _cachedProfiles = new Dictionary<string, PlayerProfile>();
        _database = new PlayerProfileDBRepository();
    }

    public PlayerProfile GetProfile(string name)
    {
        if (!_cachedProfiles.ContainsKey(name))
        {
            Console.WriteLine("Профиль игрока не найден в кеше...");
            var profile = _database.GetProfile(name);
            _cachedProfiles[name] = profile;
        }

        Console.WriteLine("Профиль игрока достается из кеша...");
        return _cachedProfiles[name];
    }

    public void UpdateHighScore(string name, int score)
    {
        if (!_cachedProfiles.ContainsKey(name))
        {
            Console.WriteLine("Профиль игрока не найден в кеше...");
            _database.UpdateHighScore(name, score);
            var profile = _database.GetProfile(name);
            _cachedProfiles[name] = profile;
            return;
        }

        // Write-through кеш
        _cachedProfiles[name].Score = score;
        _database.UpdateHighScore(name, score);
    }
}





// Адаптер для превращения оружия во врага
public class WeaponToEnemyAdapter : Enemy
{
    private readonly IWeapon _weapon;
    private readonly GameLogger _logger;
    private const float DispelProbability = 0.2f;

    public WeaponToEnemyAdapter(IWeapon weapon)
    {
        _weapon = weapon;
        _logger = GameLogger.Instance;
        Name = "Магическое оружие";
        Health = 50;
        Damage = _weapon.Damage;
    }

    public override void TakeDamage(int damage)
    {
        _logger.Log($"{Name} получает {damage} урона!");
        Health -= damage;

        float dispelRoll = new Random().NextSingle();
        if (dispelRoll <= DispelProbability)
        {
            _logger.Log("Атака рассеяла заклятие с оружия!");
            Health = 0;
        }

        if (Health > 0)
            _logger.Log($"У {Name} осталось {Health} здоровья");
    }

    public override void Attack(PlayableCharacter player)
    {
        _logger.Log($"{Name} атакует {player.Name}!");
        _weapon.Use();
        player.TakeDamage(Damage);
    }
}



// Фасад для упрощения работы с экипировкой
public class WeaponEquipmentFacade
{
    private readonly IEquipmentChest _equipmentChest;

    public WeaponEquipmentFacade(CharacterClass characterClass)
    {
        _equipmentChest = characterClass switch
        {
            CharacterClass.Warrior => new WarriorEquipmentChest(),
            CharacterClass.Mage => new MagicalEquipmentChest(),
            CharacterClass.Thief => new ThiefEquipmentChest(),
            _ => throw new ArgumentException("Неизвестный класс")
        };
    }

    // Упрощенный метод - фасад скрывает сложность выбора фабрики
    public IWeapon GetWeapon()
    {
        return _equipmentChest.GetWeapon();
    }

    // Дополнительные методы фасада могут предоставлять удобные комбинации
    public (IWeapon, IArmor) GetStarterSet()
    {
        return (_equipmentChest.GetWeapon(), _equipmentChest.GetArmor());
    }

    public string GetEquipmentDescription()
    {
        var weapon = _equipmentChest.GetWeapon();
        var armor = _equipmentChest.GetArmor();
        return $"Оружие: {weapon.GetType().Name}, Броня: {armor.GetType().Name}";
    }
}
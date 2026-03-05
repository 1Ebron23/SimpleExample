# Osa 4: Lisää testejä eri layereihin

## Tavoite

Tässä tehtävässä opit:
- **Miksi validointi tarvitaan eri layereissa** (Domain, Application, API)
- **Miten rajapinnat (interfaces) helpottavat testaamista**
- **Unit-testit vs Integration-testit**
- **Testien kirjoittaminen eri layereihin**

## Clean Architecture layereiden vastuut

```
┌─────────────────────────────────────────────┐
│  API Layer (Presentation)                   │  ← Input validointi
│  - HTTP-pyyntöjen validointi                │  ← Data format checks
│  - DTO:iden alustava tarkistus              │
└────────────────┬────────────────────────────┘
                 │
┌────────────────▼────────────────────────────┐
│  Application Layer (Business Logic)         │  ← Business sääntöjen validointi
│  - Liiketoimintalogiikan validointi         │  ← Permissions, rules
│  - Sääntöjen tarkistus                      │  ← Duplicate checks
└────────────────┬────────────────────────────┘
                 │
┌────────────────▼────────────────────────────┐
│  Domain Layer (Core)                        │  ← Entiteetin eheysvalidointi
│  - Entiteettien eheysvalidointi             │  ← Invariants
│  - Domain-sääntöjen varmistus               │  ← Business rules
└────────────────┬────────────────────────────┘
                 │
┌────────────────▼────────────────────────────┐
│  Infrastructure Layer (Data Access)         │  ← Tekninen validointi
│  - Tietokannan rajoitteiden tarkistus       │  ← Unique constraints
│  - Datan persistoinnin validointi           │  ← FK constraints
└─────────────────────────────────────────────┘
```

## Miksi validointi eri layereissa?

### 1. API Layer (Presentation) - Ensilinjan puolustus
**Mitä:** Tarkistaa että HTTP-pyyntö on kelvollinen
**Miksi:** 
- Estää roskapyynnöt aikaisin
- Antaa nopean palautteen käyttäjälle
- Ei kuormita alempia layereitä
- Varmistaa että DTO:t ovat täytetty

**Esimerkki:**
```csharp
// API Layer: Tarkista että pyyntö sisältää kaiken tarvittavan
if (string.IsNullOrEmpty(createUserDto.FirstName))
    return BadRequest("FirstName is required");
```

### 2. Application Layer - Business-logiikan validointi
**Mitä:** Tarkistaa liiketoimintasääntöjä
**Miksi:**
- Varmistaa että toiminto on sallittu
- Tarkistaa että data ei ole duplikaatti
- Soveltaa business-sääntöjä

**Esimerkki:**
```csharp
// Application Layer: Tarkista että email ei ole jo käytössä
if (await _userRepository.GetByEmailAsync(email) != null)
    throw new BusinessException("Email already exists");
```

### 3. Domain Layer - Entiteetin eheys
**Mitä:** Varmistaa että entiteetti on aina validissa tilassa
**Miksi:**
- Entiteetti ei voi koskaan olla virheellinen
- Invariantit pidetään aina voimassa
- Private setterit pakottavat validoinnin

**Esimerkki:**
```csharp
// Domain Layer: Varmista että nimi on aina vähintään 3 merkkiä
if (firstName.Length < 3)
    throw new ArgumentException("Name must be at least 3 characters");
```

---

## Vaihe 1: Lisää API-tason validointi

### 1.1 Miksi API-tason validointi?

API-layer on ensimmäinen paikka jossa data tulee järjestelmään. Täällä tehdään **alustava tarkistus**:
- Onko data ylipäätään läsnä?
- Onko data oikeassa muodossa?
- Onko pyynnön rakenne oikea?

**Tämä ei ole business-logiikkaa** - se on teknistä validointia!

### 1.2 Lisää DataAnnotations DTO:ihin

Avaa `SimpleExample.Application/DTOs/CreateUserDto.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace SimpleExample.Application.DTOs;

public class CreateUserDto
{
    [Required(ErrorMessage = "Etunimi on pakollinen")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Etunimen tulee olla 3-100 merkkiä")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sukunimi on pakollinen")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Sukunimen tulee olla 3-100 merkkiä")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sähköposti on pakollinen")]
    [EmailAddress(ErrorMessage = "Sähköpostin tulee olla kelvollinen")]
    [StringLength(255, ErrorMessage = "Sähköposti voi olla enintään 255 merkkiä")]
    public string Email { get; set; } = string.Empty;
}
```

Päivitä `SimpleExample.Application/DTOs/UpdateUserDto.cs` samalla tavalla.

### 1.3 Miksi API-validointi JA Domain-validointi?

**Kysymys:** "Eikö domain-validointi riitä?"

**Vastaus:** Ei, koska:
1. **API-validointi** antaa nopean palautteen ENNEN kuin kutsutaan domain-logiikkaa
2. **API-validointi** tarkistaa HTTP-spesifiset asiat (esim. JSON-rakenne)
3. **Domain-validointi** varmistaa että entiteetti on aina validi, riippumatta mistä data tulee
4. **Molemmat toimivat eri tasoilla** - API on "portti", Domain on "sydän"

### 1.4 Testaa API-validointi

Käynnistä sovellus ja testaa Swaggerissa:

**Testi 1: Tyhjä pyyntö**
```json
{
  "firstName": "",
  "lastName": "",
  "email": ""
}
```
→ Pitäisi palauttaa 400 Bad Request **heti**, ennen domain-validointia

---

## Vaihe 2: Lisää Application-tason validointi

### 2.1 Miksi Application-tason validointi?

Application layer sisältää **liiketoimintalogiikkaa**:
- Onko käyttäjällä oikeus toimintoon?
- Onko data jo olemassa (duplikaatit)?
- Noudattaako toiminto business-sääntöjä?

### 2.2 Lisää duplikaattitarkistus UserServiceen

Avaa `SimpleExample.Application/Services/UserService.cs`:

```csharp
public async Task<UserDto> CreateAsync(CreateUserDto createUserDto)
{
    // APPLICATION LAYER VALIDOINTI: Tarkista duplikaatti
    User? existingUser = await _userRepository.GetByEmailAsync(createUserDto.Email);
    if (existingUser != null)
    {
        throw new InvalidOperationException($"Käyttäjä sähköpostilla {createUserDto.Email} on jo olemassa");
    }

    // DOMAIN LAYER VALIDOINTI: Konstruktori validoi automaattisesti
    User user = new User(
        createUserDto.FirstName,
        createUserDto.LastName,
        createUserDto.Email
    );

    User createdUser = await _userRepository.AddAsync(user);
    return MapToDto(createdUser);
}
```

### 2.3 Päivitä Controller käsittelemään InvalidOperationException

Avaa `SimpleExample.API/Controllers/UsersController.cs`:

```csharp
[HttpPost]
public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto createUserDto)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }

    try
    {
        UserDto user = await _userService.CreateAsync(createUserDto);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Conflict(new { message = ex.Message }); // 409 Conflict
    }
}
```

### 2.4 Miksi tämä ei ole Domain-validoinnissa?

**Kysymys:** "Eikö duplikaattitarkistus kuulu Domain-layeriin?"

**Vastaus:** EI, koska:
1. Domain-layer **ei saa riippua** infrastruktuurista (repository)
2. Duplikaattitarkistus vaatii **tietokannan kyselyn**
3. Tämä on **business-sääntö**, ei entiteetin invariantti
4. Application layer **koordinoi** domain-entiteettejä ja repositoryjä

**Domain** = "Nimi tulee olla vähintään 3 merkkiä" (voidaan tarkistaa ilman ulkoisia riippuvuuksia)
**Application** = "Email ei saa olla duplikaatti" (vaatii tietokantakyselyn)

---

## Vaihe 3: Kirjoita Application-tason testit

### 3.1 Miksi rajapinnat helpottavat testaamista?

**Ongelma ilman rajapintoja:**
```csharp
// HUONO: Service riippuu suoraan repositorystä
public class UserService
{
    private readonly UserRepository _repository; // Konkreettinen luokka!
    
    // Testaaminen vaikeaa - tarvitaan oikea tietokanta!
}
```

**Ratkaisu rajapinnoilla:**
```csharp
// HYVÄ: Service riippuu rajapinnasta
public class UserService
{
    private readonly IUserRepository _repository; // Rajapinta!
    
    // Testaaminen helppoa - voidaan mockata!
}
```

**Edut:**
1. ✅ Testit eivät tarvitse oikeaa tietokantaa
2. ✅ Testit ovat nopeita (ei I/O-operaatioita)
3. ✅ Voimme simuloida eri tilanteita (virheet, poikkeukset)
4. ✅ Testit ovat eristettyjä (unit tests)

### 3.2 Asenna Moq-kirjasto

```bash
dotnet add SimpleExample.Tests package Moq
```

### 3.3 Luo UserServiceTests

Luo tiedosto `SimpleExample.Tests/Application/UserServiceTests.cs`:

```csharp
using FluentAssertions;
using Moq;
using SimpleExample.Application.DTOs;
using SimpleExample.Application.Interfaces;
using SimpleExample.Application.Services;
using SimpleExample.Domain.Entities;
using Xunit;

namespace SimpleExample.Tests.Application;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _service = new UserService(_mockRepository.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateUser()
    {
        // Arrange
        CreateUserDto dto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "matti@example.com"
        };

        // Mock: Email ei ole käytössä
        _mockRepository
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync((User?)null);

        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        // Act
        UserDto result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("Matti");
        result.LastName.Should().Be("Meikäläinen");
        result.Email.Should().Be("matti@example.com");

        // Varmista että AddAsync kutsuttiin kerran
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        CreateUserDto dto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "existing@example.com"
        };

        User existingUser = new User("Maija", "Virtanen", "existing@example.com");

        // Mock: Email on jo käytössä!
        _mockRepository
            .Setup(x => x.GetByEmailAsync(dto.Email))
            .ReturnsAsync(existingUser);

        // Act
        Func<Task> act = async () => await _service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*jo olemassa*");

        // Varmista että AddAsync EI kutsuttu
        _mockRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    // TEHTÄVÄ: Kirjoita itse testit seuraaville:
    // 1. GetByIdAsync - löytyy
    // 2. GetByIdAsync - ei löydy
    // 3. GetAllAsync - palauttaa listan
    // 4. UpdateAsync - onnistuu
    // 5. UpdateAsync - käyttäjää ei löydy
    // 6. DeleteAsync - onnistuu
    // 7. DeleteAsync - käyttäjää ei löydy
}
```

### 3.4 Tehtävä: Kirjoita loput testit itse

**Kirjoita testit seuraaville metodeille:**

1. **GetByIdAsync** - kun käyttäjä löytyy
2. **GetByIdAsync** - kun käyttäjää ei löydy (palauttaa null)
3. **GetAllAsync** - palauttaa listan käyttäjiä
4. **UpdateAsync** - onnistunut päivitys
5. **UpdateAsync** - käyttäjää ei löydy
6. **DeleteAsync** - onnistunut poisto
7. **DeleteAsync** - palauttaa false kun käyttäjää ei löydy

**Vinkki:** Käytä esimerkkitestejä mallina ja Moq-kirjaston `.Setup()` ja `.ReturnsAsync()` -metodeja.

**Tavoite:** Vähintään **9 testiä** Application-layerille (2 esimerkkiä + 7 omaa)

---

## Vaihe 4: Kirjoita API-tason testit (Controller)

### 4.1 Miksi testata Controller?

Controller on **sovelluksen julkinen rajapinta**:
- Tarkistaa että HTTP-pyynnöt käsitellään oikein
- Varmistaa että oikeat HTTP-statuskoodit palautetaan
- Testaa virhetilanteiden käsittelyn

### 4.2 Luo UsersControllerTests

Luo tiedosto `SimpleExample.Tests/API/UsersControllerTests.cs`:

```csharp
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SimpleExample.API.Controllers;
using SimpleExample.Application.DTOs;
using SimpleExample.Application.Interfaces;
using Xunit;

namespace SimpleExample.Tests.API;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _mockService;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _mockService = new Mock<IUserService>();
        _controller = new UsersController(_mockService.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOkWithUsers()
    {
        // Arrange
        List<UserDto> users = new List<UserDto>
        {
            new UserDto { Id = Guid.NewGuid(), FirstName = "Matti", LastName = "M", Email = "m@m.com" },
            new UserDto { Id = Guid.NewGuid(), FirstName = "Maija", LastName = "V", Email = "m@v.com" }
        };

        _mockService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(users);

        // Act
        ActionResult<IEnumerable<UserDto>> result = await _controller.GetAll();

        // Assert
        OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        IEnumerable<UserDto> returnedUsers = okResult.Value.Should().BeAssignableTo<IEnumerable<UserDto>>().Subject;
        returnedUsers.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_WhenUserExists_ShouldReturnOk()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        UserDto user = new UserDto { Id = userId, FirstName = "Matti", LastName = "M", Email = "m@m.com" };

        _mockService
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        ActionResult<UserDto> result = await _controller.GetById(userId);

        // Assert
        OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        UserDto returnedUser = okResult.Value.Should().BeOfType<UserDto>().Subject;
        returnedUser.Id.Should().Be(userId);
    }

    [Fact]
    public async Task GetById_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        Guid userId = Guid.NewGuid();

        _mockService
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((UserDto?)null);

        // Act
        ActionResult<UserDto> result = await _controller.GetById(userId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        CreateUserDto createDto = new CreateUserDto
        {
            FirstName = "Matti",
            LastName = "Meikäläinen",
            Email = "matti@example.com"
        };

        UserDto createdUser = new UserDto
        {
            Id = Guid.NewGuid(),
            FirstName = createDto.FirstName,
            LastName = createDto.LastName,
            Email = createDto.Email
        };

        _mockService
            .Setup(x => x.CreateAsync(createDto))
            .ReturnsAsync(createdUser);

        // Act
        ActionResult<UserDto> result = await _controller.Create(createDto);

        // Assert
        CreatedAtActionResult createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        UserDto returnedUser = createdResult.Value.Should().BeOfType<UserDto>().Subject;
        returnedUser.FirstName.Should().Be("Matti");
    }

    // TEHTÄVÄ: Kirjoita itse testit seuraaville:
    // 1. Create - InvalidOperationException (duplicate) → 409 Conflict
    // 2. Create - ArgumentException (validation) → 400 BadRequest
    // 3. Update - onnistuu → 200 OK
    // 4. Update - käyttäjää ei löydy → 404 NotFound
    // 5. Update - ArgumentException → 400 BadRequest
    // 6. Delete - onnistuu → 204 NoContent
    // 7. Delete - käyttäjää ei löydy → 404 NotFound
}
```

### 4.3 Tehtävä: Kirjoita loput Controller-testit

**Tavoite:** Vähintään **11 testiä** API-layerille (4 esimerkkiä + 7 omaa)

---

## Vaihe 5: Aja kaikki testit

### 5.1 Rakenna ja testaa

```bash
dotnet build
dotnet test
```

**Odotettu tulos:**
```
Passed!  - Failed:     0, Passed:    35+, Skipped:     0
```

(15+ domain-testiä + 9+ application-testiä + 11+ API-testiä)

### 5.2 Testaa että CI/CD toimii

```bash
git add .
git commit -m "Add multi-layer validation and comprehensive tests"
git push
```

Tarkista GitHub Actions - kaikki testit pitäisi mennä läpi!

---

## Yhteenveto: Testipyramidi

```
        ╱▔▔▔▔▔▔▔▔╲
       ╱  UI/E2E   ╲      ← Vähän (hidas, hauras)
      ╱   (ei tässä) ╲
     ╱▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔╲
    ╱  Integration    ╲   ← Kohtuullisesti (keskivaikea)
   ╱  (Infrastructure) ╲  ← (Bonus-osio)
  ╱▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔╲
 ╱      Unit Tests       ╲ ← Paljon! (nopea, helppo)
╱  Domain, App, API      ╲ ← Tässä tehtävässä
╲▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁▁╱
```

**Tässä tehtävässä keskityit Unit-testeihin:**
- ✅ Nopeat (ei I/O)
- ✅ Eristetyt (mockit)
- ✅ Helpot ylläpitää
- ✅ Paljon testejä

---

## Bonus: Infrastructure-testit (Integration Tests)

**HUOM:** Tämä on vaikeampi ja valinnainen osio!

### Miksi Infrastructure-testit ovat hankalampia?

1. Tarvitaan **oikea tietokanta** (tai in-memory database)
2. Tarvitaan **testidatan alustus**
3. Testit ovat **hitaampia** (I/O-operaatioita)
4. Testit voivat **vaikuttaa toisiinsa** (shared state)

### Esimerkki: UserRepository Integration Test

```csharp
using Microsoft.EntityFrameworkCore;
using SimpleExample.Domain.Entities;
using SimpleExample.Infrastructure.Data;
using SimpleExample.Infrastructure.Repositories;
using Xunit;

namespace SimpleExample.Tests.Infrastructure;

public class UserRepositoryIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryIntegrationTests()
    {
        // Käytä in-memory databasea testaukseen
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new UserRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddUserToDatabase()
    {
        // Arrange
        User user = new User("Matti", "Meikäläinen", "matti@example.com");

        // Act
        User result = await _repository.AddAsync(user);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        
        // Varmista että tallentui tietokantaan
        User? savedUser = await _context.Users.FindAsync(result.Id);
        Assert.NotNull(savedUser);
        Assert.Equal("Matti", savedUser.FirstName);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldFindUserByEmail()
    {
        // Arrange
        User user = new User("Matti", "Meikäläinen", "test@example.com");
        await _repository.AddAsync(user);

        // Act
        User? result = await _repository.GetByEmailAsync("test@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Matti", result.FirstName);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

**Tehtävä (Bonus):** Kirjoita integration-testit repositorylle:
- GetByIdAsync
- GetAllAsync
- UpdateAsync
- DeleteAsync
- ExistsAsync

---

## Palautettavat materiaalit

**1. GitHub Repository:**
- ✅ Kaikki muutokset pushattu
- ✅ CI/CD-workflow menee läpi (vihreä)

**2. Lähdekooditiedostot repositoryssä:**
- ✅ Päivitetyt DTO:t (DataAnnotations)
- ✅ Päivitetty UserService (duplikaattitarkistus)
- ✅ Päivitetty UsersController (virheenkäsittely)
- ✅ UserTests.cs (15+ testiä)
- ✅ UserServiceTests.cs (9+ testiä)
- ✅ UsersControllerTests.cs (11+ testiä)
- ✅ (Bonus) UserRepositoryIntegrationTests.cs

**3. Pictures-kansio kuvakaappauksilla:**

Varmista että `Pictures` -kansiossa on seuraavat kuvat (5 kpl):
- ✅ `25_All_Tests.png` - Dotnet test -tulos (35+ testiä passed täysiin pisteisiin)
- ✅ `26_Test_Explorer_Full.png` - Test Explorer Visual Studiossa (näyttää kaikki testit)
- ✅ `27_Green_Pipeline.png` - GitHub Actions - vihreä pipeline
- ✅ `28_API_Validation_Error.png` - Swagger - validointivirhe API-tasolla
- ✅ `29_Duplicate_Error.png` - Swagger - validointivirhe Application-tasolla (duplicate)

---

## Arviointikriteerit

### Erinomainen (5)
- Kaikki kolme layeriä (Domain, Application, API) testattu kattavasti
- Vähintään 35 testiä ja kaikki menevät läpi
- Ymmärtää MIKSI validointi eri layereissa
- Ymmärtää miten mockit toimivat
- Bonus: Infrastructure-testit toteutettu
- Selkeä dokumentaatio

### Hyvä (4)
- Domain ja Application layereiden testit kattavat
- Vähintään 25 testiä
- Ymmärtää mockien perusteet
- Hyvä dokumentaatio

### Tyydyttävä (3)
- Domain-testit ja osa Application-testeistä
- Vähintään 20 testiä
- Perusteet hallinnassa

### Välttävä (2)
- Osa testeistä kirjoitettu
- Vähintään 15 testiä
- Yritystä näkyy

### Hylätty (0-1)
- Testejä ei kirjoitettu tai ei mene läpi
- Ei ymmärrystä testauksesta

---

## Tärkeimmät opit

### 1. Layereiden vastuut
- **API**: Input-validointi, HTTP-käsittely
- **Application**: Business-logiikka, koordinointi
- **Domain**: Entiteetin eheys, invariantit
- **Infrastructure**: Persistointi, tekniset rajoitteet

### 2. Testattavuus
- **Rajapinnat** mahdollistavat mockauksen
- **Unit-testit** ovat nopeita ja eristettyjä
- **Integration-testit** testaavat todellista toimintaa
- **Testipyramidi**: Paljon unit-testejä, vähän E2E-testejä

### 3. Miksi testit ovat tärkeitä?
- ✅ Antavat varmuutta että koodi toimii
- ✅ Helpottavat refaktorointia
- ✅ Dokumentoivat koodin käyttäytymistä
- ✅ Estävät regressioita
- ✅ CI/CD-putki ei päästä bugeja tuotantoon

---


Muista: Testit ovat investointi. Ne maksavat itsensä takaisin kun löydät bugin ennen kuin se pääsee tuotantoon!

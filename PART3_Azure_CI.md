# Osa 3: Domain-validointi, testit ja CI/CD-putki

## Tavoite

Tässä tehtävässä lisäät sovellukseen **domain-pohjaisen validoinnin** ja **automaattiset testit**. Validointi tapahtuu suoraan domain-entiteeteissä käyttämällä konstruktoreita ja kapselia. Sen jälkeen laajennat GitHub Actions -workflowta niin, että sovellus julkaistaan Azureen **vain jos testit menevät läpi**.

## Mitä teemme?

```
Git Push → GitHub Actions → Build → Testit → ✅ Testit OK → Deploy Azureen
                                            ❌ Testit FAIL → Ei deploymenttia!
```

**Tärkeät periaatteet:**
- ✅ Validointi domain-tasolla (ei UI tai API-tasolla)
- ✅ Private setterit pakottavat käyttämään validoivia metodeja
- ✅ IDE ohjaa käyttämään oikein
- ✅ Rikkinäistä koodia ei koskaan julkaista tuotantoon

## Esivalmistelut

Varmista että olet tehnyt:
- ✅ Osa 1: Manuaalinen julkaisu
- ✅ Osa 2: GitHub Actions CD
- ✅ Sovellus on GitHubissa ja automaattinen deployment toimii

---

## Vaihe 1: Lisää domain-validointi User-entiteettiin

### 1.1 Päivitä User-entiteetti

Avaa `SimpleExample.Domain/Entities/User.cs` ja korvaa sisältö:

```csharp
namespace SimpleExample.Domain.Entities;

public class User : BaseEntity
{
    // Private setterit - vain entiteetti voi päivittää arvoja
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }

    // Paramiteriton konstruktori EF Core:a varten
    private User() 
    { 
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = string.Empty;
    }

    // Julkinen konstruktori uuden käyttäjän luomiseen
    public User(string firstName, string lastName, string email)
    {
        // Käytetään validoivia metodeja - ei koodin toistoa!
        UpdateBasicInfo(firstName, lastName);
        UpdateEmail(email);
    }

    /// <summary>
    /// Päivittää käyttäjän perustiedot (etu- ja sukunimi)
    /// </summary>
    public void UpdateBasicInfo(string firstName, string lastName)
    {
        ArgumentNullException.ThrowIfNull(firstName);
        ArgumentNullException.ThrowIfNull(lastName);
        
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("Etunimi ei voi olla tyhjä.", nameof(firstName));
        
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Sukunimi ei voi olla tyhjä.", nameof(lastName));
        
        if (firstName.Length < 3)
            throw new ArgumentException("Etunimen tulee olla vähintään 3 merkkiä pitkä.", nameof(firstName));
        
        if (lastName.Length < 3)
            throw new ArgumentException("Sukunimen tulee olla vähintään 3 merkkiä pitkä.", nameof(lastName));
        
        if (firstName.Length > 100)
            throw new ArgumentException("Etunimi voi olla enintään 100 merkkiä pitkä.", nameof(firstName));
        
        if (lastName.Length > 100)
            throw new ArgumentException("Sukunimi voi olla enintään 100 merkkiä pitkä.", nameof(lastName));
        
        FirstName = firstName;
        LastName = lastName;
    }

    /// <summary>
    /// Päivittää käyttäjän sähköpostiosoitteen
    /// </summary>
    public void UpdateEmail(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Sähköposti ei voi olla tyhjä.", nameof(email));
        
        if (!email.Contains('@'))
            throw new ArgumentException("Sähköpostin tulee olla kelvollinen.", nameof(email));
        
        if (email.Length > 255)
            throw new ArgumentException("Sähköposti voi olla enintään 255 merkkiä pitkä.", nameof(email));
        
        Email = email;
    }
}
```

### 1.2 Päivitä UserService

Avaa `SimpleExample.Application/Services/UserService.cs` ja päivitä metodit:

```csharp
public async Task<UserDto> CreateAsync(CreateUserDto createUserDto)
{
    // Konstruktori validoi automaattisesti!
    User user = new User(
        createUserDto.FirstName,
        createUserDto.LastName,
        createUserDto.Email
    );

    User createdUser = await _userRepository.AddAsync(user);
    return MapToDto(createdUser);
}

public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto updateUserDto)
{
    User? user = await _userRepository.GetByIdAsync(id);
    if (user == null)
    {
        return null;
    }

    // UpdateBasicInfo ja UpdateEmail validoivat automaattisesti!
    user.UpdateBasicInfo(updateUserDto.FirstName, updateUserDto.LastName);
    user.UpdateEmail(updateUserDto.Email);

    User updatedUser = await _userRepository.UpdateAsync(user);
    return MapToDto(updatedUser);
}
```

### 1.3 Päivitä GenericRepository

Avaa `SimpleExample.Infrastructure/Repositories/GenericRepository.cs` ja päivitä `AddAsync`:

```csharp
public async Task<T> AddAsync(T entity)
{
    // EI aseteta Id:tä, FirstName, LastName, Email - entity on jo validi!
    entity.Id = Guid.NewGuid();
    entity.CreatedAt = DateTime.UtcNow;
    entity.UpdatedAt = DateTime.UtcNow;
    
    await _dbSet.AddAsync(entity);
    await _context.SaveChangesAsync();
    
    return entity;
}
```

### 1.4 Päivitä InMemoryUserRepository

Avaa `SimpleExample.Infrastructure/Repositories/InMemoryUserRepository.cs` ja päivitä InitializeSampleData:

```csharp
private void InitializeSampleData()
{
    DateTime now = DateTime.UtcNow;

    // Käytä konstruktoria käyttäjien luomiseen
    User user1 = new User("Matti", "Meikäläinen", "matti.meikalainen@example.com");
    user1.Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    user1.CreatedAt = now.AddDays(-30);
    user1.UpdatedAt = now.AddDays(-30);

    User user2 = new User("Maija", "Virtanen", "maija.virtanen@example.com");
    user2.Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
    user2.CreatedAt = now.AddDays(-15);
    user2.UpdatedAt = now.AddDays(-5);

    User user3 = new User("Teppo", "Testaaja", "teppo.testaaja@example.com");
    user3.Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
    user3.CreatedAt = now.AddDays(-7);
    user3.UpdatedAt = now.AddDays(-1);

    _users.AddRange(new[] { user1, user2, user3 });
}
```

**HUOM:** Joudut lisäämään BaseEntity-luokkaan internal/public setterit Id, CreatedAt ja UpdatedAt -properteihin, tai tekemään ne testattavaksi.

### 1.5 Päivitä UsersController käsittelemään virheet

Avaa `SimpleExample.API/Controllers/UsersController.cs` ja lisää try-catch:

```csharp
[HttpPost]
public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto createUserDto)
{
    try
    {
        UserDto user = await _userService.CreateAsync(createUserDto);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}

[HttpPut("{id}")]
public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UpdateUserDto updateUserDto)
{
    try
    {
        UserDto? user = await _userService.UpdateAsync(id, updateUserDto);
        if (user == null)
        {
            return NotFound(new { message = $"User with ID {id} not found" });
        }
        return Ok(user);
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}
```

### 1.6 Testaa validointi lokaalisti

Käynnistä sovellus:
```bash
dotnet run --project SimpleExample.API
```

Avaa Swagger: `https://localhost:5001/swagger`

**Testaa validointi:**

1. **POST /api/users** - Yritä luoda käyttäjä liian lyhyellä nimellä:
```json
{
  "firstName": "AB",
  "lastName": "XY",
  "email": "invalid-email"
}
```

Pitäisi palauttaa **400 Bad Request**:
```json
{
  "message": "Etunimen tulee olla vähintään 3 merkkiä pitkä."
}
```

2. **POST /api/users** - Yritä kelvollisella datalla:
```json
{
  "firstName": "Matti",
  "lastName": "Meikäläinen",
  "email": "matti@example.com"
}
```

Pitäisi palauttaa **201 Created** ✅

---

## Vaihe 2: Luo xUnit-testiprojekti

### 2.1 Luo testiprojekti

```bash
dotnet new xunit -n SimpleExample.Tests -o SimpleExample.Tests -f net9.0
```

### 2.2 Lisää projektireferenssit

```bash
dotnet add SimpleExample.Tests reference SimpleExample.Domain
dotnet add SimpleExample.Tests reference SimpleExample.Application
dotnet add SimpleExample.Tests reference SimpleExample.Infrastructure
```

### 2.3 Lisää testiprojekti solutioon

```bash
dotnet sln add SimpleExample.Tests/SimpleExample.Tests.csproj
```

### 2.4 Asenna tarvittavat paketit

```bash
dotnet add SimpleExample.Tests package FluentAssertions
```

### 2.5 Poista oletustesti

Poista tiedosto `SimpleExample.Tests/UnitTest1.cs`

---

## Vaihe 3: Kirjoita domain-validointitestit

### 3.1 Luo UserTests

Luo tiedosto `SimpleExample.Tests/Domain/UserTests.cs`:

```csharp
using FluentAssertions;
using SimpleExample.Domain.Entities;
using Xunit;

namespace SimpleExample.Tests.Domain;

public class UserTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateUser()
    {
        // Act
        User user = new User("Matti", "Meikäläinen", "matti@example.com");

        // Assert
        user.Should().NotBeNull();
        user.FirstName.Should().Be("Matti");
        user.LastName.Should().Be("Meikäläinen");
        user.Email.Should().Be("matti@example.com");
    }

    [Fact]
    public void Constructor_WithEmptyFirstName_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => new User("", "Meikäläinen", "test@test.com");

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Etunimi ei voi olla tyhjä*");
    }

    [Fact]
    public void Constructor_WithTooShortFirstName_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => new User("AB", "Meikäläinen", "test@test.com");

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Etunimen tulee olla vähintään 3 merkkiä pitkä*");
    }

    [Fact]
    public void Constructor_WithEmptyLastName_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => new User("Matti", "", "test@test.com");

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Sukunimi ei voi olla tyhjä*");
    }

    [Fact]
    public void Constructor_WithTooShortLastName_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => new User("Matti", "XY", "test@test.com");

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Sukunimen tulee olla vähintään 3 merkkiä pitkä*");
    }

    [Fact]
    public void Constructor_WithInvalidEmail_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => new User("Matti", "Meikäläinen", "invalid-email");

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Sähköpostin tulee olla kelvollinen*");
    }

    [Fact]
    public void Constructor_WithNullFirstName_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new User(null!, "Meikäläinen", "test@test.com");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("Mat")]  // Minimiraja 3 merkkiä
    [InlineData("Matti")]
    [InlineData("MattiJohannes")]
    public void Constructor_WithValidFirstNameLengths_ShouldSucceed(string firstName)
    {
        // Act
        User user = new User(firstName, "Meikäläinen", "test@test.com");

        // Assert
        user.FirstName.Should().Be(firstName);
    }

    [Fact]
    public void UpdateBasicInfo_WithValidData_ShouldUpdateUser()
    {
        // Arrange
        User user = new User("Matti", "Meikäläinen", "matti@example.com");

        // Act
        user.UpdateBasicInfo("Maija", "Virtanen");

        // Assert
        user.FirstName.Should().Be("Maija");
        user.LastName.Should().Be("Virtanen");
    }

    [Fact]
    public void UpdateBasicInfo_WithTooShortFirstName_ShouldThrowArgumentException()
    {
        // Arrange
        User user = new User("Matti", "Meikäläinen", "matti@example.com");

        // Act
        Action act = () => user.UpdateBasicInfo("AB", "Virtanen");

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Etunimen tulee olla vähintään 3 merkkiä pitkä*");
    }

    [Fact]
    public void UpdateEmail_WithValidEmail_ShouldUpdateEmail()
    {
        // Arrange
        User user = new User("Matti", "Meikäläinen", "matti@example.com");

        // Act
        user.UpdateEmail("uusi@example.com");

        // Assert
        user.Email.Should().Be("uusi@example.com");
    }

    [Fact]
    public void UpdateEmail_WithInvalidEmail_ShouldThrowArgumentException()
    {
        // Arrange
        User user = new User("Matti", "Meikäläinen", "matti@example.com");

        // Act
        Action act = () => user.UpdateEmail("invalid-email");

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Sähköpostin tulee olla kelvollinen*");
    }
}
```

---

## Vaihe 4: Aja testit lokaalisti

### 4.1 Rakenna kaikki projektit

```bash
dotnet build
```

Varmista että **0 errors**.

### 4.2 Aja testit

```bash
dotnet test
```

**Odotettu tulos:**
```
Passed!  - Failed:     0, Passed:    10, Skipped:     0, Total:    10
```

Jos testit epäonnistuvat, tarkista:
- Validaattoreiden virheilmoitukset täsmäävät
- DTO:t ovat oikein
- Paketit on asennettu

### 4.3 Aja testit yksityiskohtaisesti

```bash
dotnet test --verbosity detailed
```

Näet jokaisen testin erikseen.

### 4.4 Aja vain tietty testiluokka

```bash
dotnet test --filter "FullyQualifiedName~CreateUserDtoValidatorTests"
```

---

## Vaihe 5: Päivitä GitHub Actions workflow (CI + CD)

### 5.1 Avaa workflow-tiedosto

Avaa `.github/workflows/azure-deploy.yml`

### 5.2 Päivitä workflow sisältö

Korvaa koko tiedoston sisältö:

```yaml
name: CI/CD Pipeline

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]
  workflow_dispatch:

env:
  DOTNET_VERSION: '9.0.x'
  AZURE_WEBAPP_NAME: 'SINUN-APP-SERVICE-NIMI'  # Vaihda tähän!

jobs:
  # Job 1: Build ja testit (CI)
  build-and-test:
    runs-on: ubuntu-latest
    name: Build and Test
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --configuration Release --no-restore
    
    - name: Run tests
      run: dotnet test --configuration Release --no-build --verbosity normal
    
    - name: Publish
      run: dotnet publish SimpleExample.API/SimpleExample.API.csproj -c Release -o ./publish --no-build
    
    # Tallenna julkaisupaketit seuraavaa jobia varten
    - name: Upload artifact for deployment
      uses: actions/upload-artifact@v4
      with:
        name: app-package
        path: ./publish

  # Job 2: Deploy Azureen (CD) - Ajetaan VAIN jos testit menivät läpi
  deploy-to-azure:
    runs-on: ubuntu-latest
    name: Deploy to Azure
    needs: build-and-test  # Odottaa että build-and-test on onnistunut!
    if: github.ref == 'refs/heads/main' && github.event_name == 'push'
    
    steps:
    - name: Download artifact from build job
      uses: actions/download-artifact@v4
      with:
        name: app-package
        path: ./publish
    
    - name: Deploy to Azure Web App
      uses: azure/webapps-deploy@v3
      with:
        app-name: ${{ env.AZURE_WEBAPP_NAME }}
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

**Tärkeää muista:**
- Vaihda `AZURE_WEBAPP_NAME` omaan nimeen!

### 5.3 Mitä muuttui?

**Aiemmin:**
- Yksi job: build + deploy

**Nyt:**
- **Job 1 (build-and-test)**: Rakennus + testit
- **Job 2 (deploy-to-azure)**: Deployment (ajetaan vain jos Job 1 onnistui!)

**Logiikka:**
```
Testit OK ✅ → Deploy Azureen
Testit FAIL ❌ → Ei deploymenttia!
```

---

## Vaihe 6: Testaa CI/CD-putki

### 6.1 Committaa muutokset

```bash
git add .
git commit -m "Add validation and tests with CI/CD pipeline"
git push
```

### 6.2 Seuraa GitHub Actionsissa

1. GitHub → **Actions**
2. Näet uuden workflow runin "CI/CD Pipeline"
3. Klikkaa sitä

**Huomaa:**
- Näkyy **2 jobia**: `build-and-test` ja `deploy-to-azure`
- `build-and-test` alkaa heti
- `deploy-to-azure` odottaa että `build-and-test` valmistuu

### 6.3 Tarkista testien tulos

Klikkaa **build-and-test** -jobin "Run tests" -vaihetta.

Pitäisi näkyä:
```
Passed!  - Failed:     0, Passed:    10, Skipped:     0, Total:    10
```

### 6.4 Tarkista deployment

Kun `build-and-test` on valmis ✅, `deploy-to-azure` käynnistyy automaattisesti.

Odota että deployment valmistuu.

### 6.5 Testaa sovellus Azuressa

Avaa: `https://SINUN-APP.azurewebsites.net/swagger`

Testaa validointi tuotannossa:
1. Yritä luoda käyttäjä liian lyhyellä nimellä → Pitäisi palauttaa 400 Bad Request
2. Luo käyttäjä kelvollisella datalla → Pitäisi onnistua

---

## Vaihe 7: Testaa epäonnistuva testi

### 7.1 Riko testi tarkoituksella

Avaa `SimpleExample.Tests/Validators/CreateUserDtoValidatorTests.cs` ja muuta yksi testi:

```csharp
[Fact]
public void Should_Have_Error_When_FirstName_Is_Empty()
{
    CreateUserDto dto = new CreateUserDto { FirstName = "", LastName = "Meikäläinen", Email = "test@test.com" };
    var result = _validator.TestValidate(dto);
    
    // VÄÄRÄ ODOTUS - testi epäonnistuu!
    result.ShouldNotHaveAnyValidationErrors();
}
```

### 7.2 Pushaa GitHubiin

```bash
git add .
git commit -m "Test failing tests in CI"
git push
```

### 7.3 Seuraa GitHub Actionsissa

1. GitHub → **Actions**
2. `build-and-test` -job **epäonnistuu** ❌
3. `deploy-to-azure` **ei käynnisty ollenkaan!** ✅

**Tämä on oikea toiminta!** Rikkinäistä koodia ei julkaista.

### 7.4 Korjaa testi

Palauta testi takaisin oikeaksi:

```csharp
[Fact]
public void Should_Have_Error_When_FirstName_Is_Empty()
{
    CreateUserDto dto = new CreateUserDto { FirstName = "", LastName = "Meikäläinen", Email = "test@test.com" };
    var result = _validator.TestValidate(dto);
    result.ShouldHaveValidationErrorFor(x => x.FirstName)
          .WithErrorMessage("Etunimi on pakollinen");
}
```

Pushaa:
```bash
git add .
git commit -m "Fix test"
git push
```

Nyt `build-and-test` menee läpi ✅ ja `deploy-to-azure` ajetaan!

---

## Vaihe 8: Dokumentoi

### Ota kuvakaappaukset ja tallenna ne `Pictures` -kansioon:

Varmista että `Pictures` -kansio on olemassa projektin juuressa.

**Tallenna seuraavat kuvakaappaukset:**

1. `17_Test_Explorer.png` - Validointitestit Visual Studiossa (Test Explorer)
2. `18_Dotnet_Test.png` - Dotnet test -komennon tulos PowerShellissä (näyttää passed tests)
3. `19_CI_CD_Pipeline.png` - GitHub Actions - CI/CD Pipeline (molemmat jobit vihreänä)
4. `20_Build_Test_Log.png` - build-and-test job log (testit näkyy läpimenneinä)
5. `21_Deploy_Log.png` - deploy-to-azure job log (deployment onnistui)
6. `22_Failed_Workflow.png` - GitHub Actions - epäonnistunut workflow (kun testi rikki, deploy ei aja)
7. `23_Swagger_Validation_Error.png` - Swagger UI - validointivirhe (400 Bad Request liian lyhyellä nimellä)
8. `24_Swagger_Success.png` - Swagger UI - onnistunut luonti (201 Created kelvollisella datalla)

### GitHub Repository

Pushaa kaikki muutokset GitHubiin:
```bash
git add .
git commit -m "Add domain validation and tests with CI/CD"
git push
```

Varmista että GitHub Actions workflow menee läpi!

---

## Vianmääritys

### Ongelma: "Could not find project or directory"

**Ratkaisu:** Varmista että testiprojekti on lisätty solutioon:
```bash
dotnet sln add SimpleExample.Tests/SimpleExample.Tests.csproj
```

### Ongelma: BaseEntity private setterit

**Ratkaisu:** Jos joudut testaamaan tai alustamaan käyttäjiä, voit lisätä BaseEntity-luokkaan:
```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

Tai tehdä setterit `internal`:
```csharp
public Guid Id { get; internal set; }
```

### Ongelma: "Upload artifact failed"

**Ratkaisu:** Varmista että `./publish` -kansio on olemassa ennen upload-vaihetta:
```yaml
- name: Publish
  run: dotnet publish ... -o ./publish
```

### Ongelma: Deploy-job ei käynnisty vaikka testit menee läpi

**Ratkaisu:** Tarkista workflow:
```yaml
needs: build-and-test  # Tämä rivi täytyy olla!
if: github.ref == 'refs/heads/main' && github.event_name == 'push'
```

### Ongelma: Validointi ei toimi Azuressa

**Ratkaisu:** Varmista että `UsersController.cs`:ssä on try-catch:
```csharp
try
{
    UserDto user = await _userService.CreateAsync(createUserDto);
    return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
}
catch (ArgumentException ex)
{
    return BadRequest(new { message = ex.Message });
}
```

---

## Palautettavat materiaalit

**1. GitHub Repository:**
- ✅ Kaikki lähdekooditiedostot repositoryssä:
  - Päivitetty `User.cs` (domain validointi konstruktorissa)
  - Päivitetty `UserService.cs`
  - Päivitetty `UsersController.cs` (try-catch)
  - Testitiedosto `UserTests.cs`
  - Päivitetty `azure-deploy.yml`

**2. Pictures-kansio kuvakaappauksilla:**

Varmista että `Pictures` -kansiossa on seuraavat kuvat (8 kpl):
- ✅ `17_Test_Explorer.png` - Test Explorer Visual Studiossa
- ✅ `18_Dotnet_Test.png` - Lokaalisti ajetut testit (dotnet test -tulos, 15+ testiä)
- ✅ `19_CI_CD_Pipeline.png` - GitHub Actions CI/CD Pipeline (2 jobia vihreänä)
- ✅ `20_Build_Test_Log.png` - build-and-test job -loki (testit näkyy)
- ✅ `21_Deploy_Log.png` - deploy-to-azure job -loki
- ✅ `22_Failed_Workflow.png` - Epäonnistunut workflow (deploy ei aja kun testi fail)
- ✅ `23_Swagger_Validation_Error.png` - Swagger validointivirhe (400 Bad Request)
- ✅ `24_Swagger_Success.png` - Swagger onnistunut luonti (201 Created)


---

## Arviointikriteerit

### Erinomainen (5)
- Domain-validointi toteutettu oikein (private setterit, validoiva konstruktori)
- Konstruktori validoi automaattisesti ja UpdateBasicInfo/UpdateEmail -metodit toimivat
- Vähintään 15 testiä kirjoitettu ja kaikki menevät läpi
- CI/CD-putki toimii oikein: testit ensin, deploy vasta jos testit OK
- Testattu että deployment ei tapahdu jos testit epäonnistuvat
- Kaikki dokumentaatio ja kuvakaappaukset mukana
- Selitys domain-validoinnin eduista

### Hyvä (4)
- Domain-validointi toteutettu
- Vähintään 12 testiä ja kaikki menevät läpi
- CI/CD-putki toimii
- Hyvä dokumentaatio

### Tyydyttävä (3)
- Validointi toimii perustasolla domain-tasolla
- Vähintään 8 testiä
- CI/CD-putki toimii (pieniä ongelmia hyväksytään)
- Perusdokumentaatio mukana

### Välttävä (2)
- Validointi toteutettu mutta ei toimi täydellisesti
- Muutamia testejä kirjoitettu
- CI/CD yritetty

### Hylätty (0-1)
- Validointi puuttuu tai ei toimi
- Ei testejä
- CI/CD ei toimi

---

## Yhteenveto

Olet nyt rakentanut tuotantotasoisen CI/CD-putken domain-pohjaisella validoinnilla:

```
📝 Koodimuutos
    ↓
💾 Git Push
    ↓
🔨 GitHub Actions: Build
    ↓
✅ Testit (TÄRKEÄ!)
    ↓
✅ Testit OK → ☁️ Deploy Azureen
❌ Testit FAIL → 🛑 Ei deploymenttia!
```

**Tärkeimmät opit:**
- ✅ **Domain validointi** - Validointi kuuluu business logiikkaan, ei UI:hin
- ✅ **Kapselointi** - Private setterit pakottavat käyttämään validoivia metodeja
- ✅ **IDE tuki** - Kehittäjä ei voi vahingossa rikkoa sääntöjä
- ✅ **Testattavuus** - Domain-logiikka on helppo testata ilman riippuvuuksia
- ✅ **CI/CD** - Automaattinen prosessi varmistaa laadun
- ✅ **Rikkinäistä koodia ei julkaista tuotantoon!**

**Tämä on ammattimainen Clean Architecture -lähestymistapa!** 

---


Jos kohtaat ongelmia, tarkista vianmääritys-osio tai testaa ensin lokaalisti komennolla `dotnet test`.

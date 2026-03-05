# Osa 1: Sovelluksen julkaiseminen Azureen manuaalisesti

## Tavoite

Tässä tehtävässä opit julkaisemaan ASP.NET Core Web API -sovelluksen Azure App Serviceen kahdella eri tavalla:
1. **Visual Studion graafisen käyttöliittymän kautta**
2. **Azure CLI:n avulla komentoriviltä**

## Esivalmistelut

Varmista, että sinulla on:

- ✅ Visual Studio 2022 asennettuna
- ✅ Azure-tili (opiskelija- tai ilmainen kokeilutili)
- ✅ Azure CLI asennettuna ([lataa täältä](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli))
- ✅ **App Service on luotu Azureen valmiiksi** (opettaja on tehnyt tämän)

## Tärkeää tietoa sovelluksesta

Sovellus käyttää **in-memory tietokantaa**, joten sinun ei tarvitse luoda oikeaa tietokantaa. Sovellus sisältää valmiiksi 3 käyttäjää:

1. Matti Meikäläinen (matti.meikalainen@example.com)
2. Maija Virtanen (maija.virtanen@example.com)
3. Teppo Testaaja (teppo.testaaja@example.com)

**HUOM:** Data häviää kun sovellus käynnistetään uudelleen, mutta 3 alkuperäistä käyttäjää palautuvat automaattisesti.

---

## Menetelmä 1: Julkaisu Visual Studion kautta

### Vaihe 1: Avaa projekti Visual Studiossa

1. Avaa **SimpleExample.sln** Visual Studio 2022:ssa
2. Varmista, että projekti kääntyy ilman virheitä:
   - Klikkaa **Build** → **Rebuild Solution**
   - Tarkista Output-ikkunasta että build onnistuu

### Vaihe 2: Varmista In-Memory -tila

Avaa `SimpleExample.API/appsettings.json` ja tarkista että `UseInMemoryDatabase` on `true`:

```json
{
  "UseInMemoryDatabase": true,
  ...
}
```

Jos arvo on `false`, vaihda se `true`:ksi ja tallenna tiedosto.

### Vaihe 3: Avaa Publish-ikkuna

1. **Solution Explorer** -ikkunassa, klikkaa hiiren oikealla **SimpleExample.API** -projektia
2. Valitse **Publish...**

![Publish-valikon avaaminen]

### Vaihe 4: Valitse julkaisukohde

**Ensimmäisellä kerralla (uusi profiili):**

1. Valitse **Azure** → Klikkaa **Next**
2. Valitse **Azure App Service (Windows)** → Klikkaa **Next**
3. **Kirjaudu Azure-tilillesi** jos et ole vielä kirjautunut (klikkaa Sign in)

### Vaihe 5: Valitse olemassa oleva App Service

1. Valitse oikea **Subscription** (tilaus)
2. **View** -pudotusvalikosta valitse **Resource group**
3. Etsi ja valitse **sinun App Service -instanssisi** listasta
4. Klikkaa **Finish**

**TÄRKEÄÄ:** Älä luo uutta App Servicea, vaan valitse olemassa oleva!

### Vaihe 6: Tarkista Publish-profiilin asetukset

1. Kun profiili on luotu, näet **Publish**-sivun
2. Klikkaa **Show all settings** (tai hammasratas-ikonia)
3. Tarkista asetukset:
   - **Configuration**: Release
   - **Target Framework**: net9.0
   - **Target Runtime**: Portable
4. Klikkaa **Save**

### Vaihe 7: Julkaise sovellus

1. Klikkaa **Publish**-painiketta (suuri sininen painike)
2. Seuraa edistymistä **Output**-ikkunasta:
   - Build käynnistyy
   - Tiedostot pakataan
   - Upload Azureen
   - Deployment valmistuu
3. Kun näet "Publish Succeeded", selain avautuu automaattisesti

**Odota 30-60 sekuntia** että sovellus käynnistyy Azuressa!

### Vaihe 8: Testaa sovellusta

1. Kun selain avautuu, lisää URL:iin `/swagger`:
   ```
   https://SINUN-APPSERVICE.azurewebsites.net/swagger
   ```

2. **Testaa GET /api/users:**
   - Klikkaa "GET /api/users"
   - Klikkaa "Try it out"
   - Klikkaa "Execute"
   - **Pitäisi palauttaa 3 käyttäjää!**

3. **Testaa POST /api/users (Luo uusi käyttäjä):**
   - Klikkaa "POST /api/users"
   - Klikkaa "Try it out"
   - Syötä Request body:
   ```json
   {
     "firstName": "Testi",
     "lastName": "Käyttäjä",
     "email": "testi@example.com"
   }
   ```
   - Klikkaa "Execute"
   - Tarkista että saat 201 Created -vastauksen

4. **Testaa GET /api/users uudelleen:**
   - Nyt pitäisi näkyä 4 käyttäjää (3 alkuperäistä + uusi)

5. **Testaa GET /api/users/{id}:**
   - Kopioi jonkin käyttäjän ID
   - Testaa GET-pyyntöä kyseisellä ID:llä

6. **Testaa PUT ja DELETE:**
   - Päivitä käyttäjän tiedot (PUT)
   - Poista käyttäjä (DELETE)

### Vaihe 9: Dokumentoi

**Ota kuvakaappaukset ja tallenna ne `Pictures` -kansioon:**

Luo kansio `Pictures` projektin juureen (jos ei ole vielä olemassa).

**Tallenna seuraavat kuvakaappaukset:**
1. `01_VS_Publish_Profile.png` - Visual Studio Publish-profiili
2. `02_VS_Publish_Output.png` - Publish-prosessin Output-ikkuna (onnistunut deployment)
3. `03_Swagger_Azure.png` - Swagger UI Azuressa
4. `04_GET_Users.png` - GET /api/users -vastaus (näyttää käyttäjät)
5. `05_POST_User.png` - POST /api/users -vastaus (uuden käyttäjän luonti)

---

## Menetelmä 2: Julkaisu Azure CLI:n kautta

### Vaihe 1: Asenna Azure CLI (jos ei ole vielä)

**Windows:**
```powershell
winget install Microsoft.AzureCLI
```

Tai lataa: https://aka.ms/installazurecliwindows

**Tarkista asennus:**
```bash
az --version
```

### Vaihe 2: Kirjaudu Azureen

Avaa **PowerShell** tai **Command Prompt**:

```bash
az login
```

- Selain avautuu
- Kirjaudu Azure-tilillesi
- Palaa komentoriville

**Tarkista oikea tilaus:**
```bash
az account show
```

Jos sinulla on useita tilauksia, vaihda oikea:
```bash
az account list --output table
az account set --subscription "TILAUKSEN_NIMI"
```

### Vaihe 3: Määritä muuttujat

**PowerShell:**
```powershell
$resourceGroup = "SINUN-RESOURCE-GROUP-NIMI"
$appServiceName = "SINUN-APPSERVICE-NIMI"
```

**CMD (Command Prompt):**
```cmd
set resourceGroup=SINUN-RESOURCE-GROUP-NIMI
set appServiceName=SINUN-APPSERVICE-NIMI
```

**HUOM:** Kysy opettajalta tai tarkista Azure Portalista:
- Resource Group -nimi
- App Service -nimi

### Vaihe 4: Tarkista App Service

Varmista että App Service on olemassa:

**PowerShell:**
```powershell
az webapp show --name $appServiceName --resource-group $resourceGroup
```

**CMD:**
```cmd
az webapp show --name %appServiceName% --resource-group %resourceGroup%
```

Jos saat virheen, tarkista nimet!

### Vaihe 5: Rakenna sovellus

Navigoi projektin juurihakemistoon:

```bash
cd C:\Users\SINUN-KÄYTTÄJÄ\source\repos\SimpleExample
```

Rakenna sovellus:

```bash
dotnet publish SimpleExample.API\SimpleExample.API.csproj --configuration Release --output .\publish
```

**Mitä tapahtuu:**
- Sovellus käännetään Release-tilassa
- Kaikki tarvittavat tiedostot kopioidaan `publish`-kansioon
- Prosessi kestää 10-30 sekuntia

### Vaihe 6: Luo ZIP-paketti

**PowerShell:**
```powershell
Compress-Archive -Path .\publish\* -DestinationPath .\app.zip -Force
```

**CMD (tarvitset 7-Zip tai WinRAR):**
- Avaa `publish`-kansio
- Valitse kaikki tiedostot
- Pakkaa nimellä `app.zip` projektin juureen

**Tarkista pakettin koko:**
```powershell
(Get-Item .\app.zip).Length / 1MB
# Pitäisi olla noin 30-50 MB
```

### Vaihe 7: Lähetä Azureen

**PowerShell:**
```powershell
az webapp deployment source config-zip `
  --name $appServiceName `
  --resource-group $resourceGroup `
  --src app.zip
```

**CMD:**
```cmd
az webapp deployment source config-zip ^
  --name %appServiceName% ^
  --resource-group %resourceGroup% ^
  --src app.zip
```

**Mitä tapahtuu:**
- ZIP-paketti ladataan Azureen (voi kestää 1-3 minuuttia)
- Azure purkaa paketin
- Sovellus käynnistetään automaattisesti
- Näet JSON-vastauksen kun valmis

### Vaihe 8: Käynnistä sovellus uudelleen

**PowerShell:**
```powershell
az webapp restart --name $appServiceName --resource-group $resourceGroup
```

**CMD:**
```cmd
az webapp restart --name %appServiceName% --resource-group %resourceGroup%
```

### Vaihe 9: Avaa sovellus

**PowerShell:**
```powershell
az webapp browse --name $appServiceName --resource-group $resourceGroup
```

**CMD:**
```cmd
az webapp browse --name %appServiceName% --resource-group %resourceGroup%
```

Tai manuaalisesti selaimessa:
```
https://SINUN-APPSERVICE.azurewebsites.net/swagger
```

### Vaihe 10: Testaa sovellus

Testaa täsmälleen samat asiat kuin Visual Studio -menetelmässä (Vaihe 8):

1. ✅ GET /api/users (3 käyttäjää)
2. ✅ POST /api/users (luo uusi)
3. ✅ GET /api/users/{id} (hae yksittäinen)
4. ✅ PUT /api/users/{id} (päivitä)
5. ✅ DELETE /api/users/{id} (poista)

### Vaihe 11: Katso lokeja (valinnainen)

Jos kohtaat ongelmia, voit seurata lokeja reaaliajassa:

**PowerShell/CMD:**
```bash
az webapp log tail --name %appServiceName% --resource-group %resourceGroup%
```

Pysäytä näppäinyhdistelmällä **Ctrl+C**.

### Vaihe 12: Siivoa väliaikaiset tiedostot

**PowerShell:**
```powershell
Remove-Item .\publish -Recurse -Force
Remove-Item .\app.zip
```

### Vaihe 13: Dokumentoi

**Ota kuvakaappaukset ja tallenna ne `Pictures` -kansioon:**

**Tallenna seuraavat kuvakaappaukset:**
1. `06_CLI_Commands.png` - PowerShell/CMD-ikkuna jossa näkyy käytetyt komennot
2. `07_CLI_Publish_Output.png` - `dotnet publish` -komennon output
3. `08_CLI_Deployment.png` - `az webapp deployment` -komennon onnistunut vastaus
4. `09_CLI_Swagger.png` - Swagger UI (sama kuin Visual Studio -menetelmässä)


---

## Bonus: Luo PowerShell-skripti

Automatisoi deployment luomalla skripti (eli tekee tuon automaattiseti, joka aiemmassa osiossa tehtiin) `deploy.ps1`:

```powershell
# ====================================
# Azure Deployment Script
# ====================================

param(
    [Parameter(Mandatory=$true)]
    [string]$resourceGroup,
    
    [Parameter(Mandatory=$true)]
    [string]$appServiceName
)

Write-Host "Building application..." -ForegroundColor Cyan
dotnet publish SimpleExample.API\SimpleExample.API.csproj --configuration Release --output .\publish

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Creating ZIP package..." -ForegroundColor Cyan
if (Test-Path .\app.zip) { Remove-Item .\app.zip }
Compress-Archive -Path .\publish\* -DestinationPath .\app.zip -Force

Write-Host "Deploying to Azure..." -ForegroundColor Cyan
az webapp deployment source config-zip `
  --name $appServiceName `
  --resource-group $resourceGroup `
  --src app.zip

if ($LASTEXITCODE -ne 0) {
    Write-Host "Deployment failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Restarting app..." -ForegroundColor Cyan
az webapp restart --name $appServiceName --resource-group $resourceGroup

Write-Host "`nDeployment complete!" -ForegroundColor Green
Write-Host "URL: https://$appServiceName.azurewebsites.net/swagger" -ForegroundColor Green

# Cleanup
Remove-Item .\publish -Recurse -Force
Remove-Item .\app.zip
```

**Käytä skriptiä:**
```powershell
.\deploy.ps1 -resourceGroup "SINUN-RG" -appServiceName "SINUN-APP"
```

---

## Palautettavat materiaalit

**1. Pictures-kansio kuvakaappauksilla:**

 `Pictures` -kansioon tallenna sinne kaikki kuvakaappaukset:

**Visual Studio -julkaisu:**
- ✅ `01_VS_Publish_Profile.png` - Visual Studio Publish-profiili
- ✅ `02_VS_Publish_Output.png` - Publish-prosessin Output-ikkuna
- ✅ `03_Swagger_Azure.png` - Swagger UI Azuressa
- ✅ `04_GET_Users.png` - GET /api/users -vastaus (3 käyttäjää)
- ✅ `05_POST_User.png` - POST /api/users -vastaus (uusi käyttäjä)

**Azure CLI -julkaisu:**
- ✅ `06_CLI_Commands.png` - PowerShell/CMD-ikkuna käytetyillä komennoilla
- ✅ `07_CLI_Publish_Output.png` - `dotnet publish` -komennon output
- ✅ `08_CLI_Deployment.png` - `az webapp deployment` -komennon vastaus
- ✅ `09_CLI_Swagger.png` - Swagger UI

**Azure Portal:**
- ✅ `10_Azure_Portal.png` - App Service Overview -sivu Azure Portalissa


**3. PowerShell-skripti (deploy.ps1) - BONUS:**
- ✅ Toimiva automatisoitu deployment-skripti. Tallenna scripti projektin juureen scripts kansion alle

---

## Arviointikriteerit

### Erinomainen (5)
- Molemmat julkaisumenetelmät toimivat virheettömästi
- Kaikki vaaditut kuvakaappaukset mukana
- PowerShell-skripti mukana ja toimii
- deploy-commands.txt tiedosto selkeä ja kommentoitu
- Sovellus toimii Azuressa

### Hyvä (4)
- Molemmat menetelmät toimivat
- Vaaditut kuvakaappaukset mukana
- deploy-commands.txt mukana

### Tyydyttävä (3)
- Molemmat menetelmät toteutettu (pieniä ongelmia hyväksytään)
- Perus kuvakaappaukset mukana
- Sovellus toimii Azuressa

### Välttävä (2)
- Yksi menetelmä toimii kunnolla
- Osa kuvakaappauksista puuttuu

### Hylätty (0-1)
- Sovellus ei julkaistu onnistuneesti
- Ei todisteta että sovellus toimii Azuressa

---

## Tuki ja vinkit

### Yleisimmät ongelmat:

**1. "Could not find a part of the path"**
- Varmista että olet oikeassa hakemistossa
- Tarkista että polut ovat oikein (käytä Tab-näppäintä täydennykseen)

**2. "Authentication failed"**
- Suorita `az login` uudelleen
- Tarkista että oikea tilaus on valittuna

**3. "Resource not found"**
- Tarkista Resource Group ja App Service -nimet
- Varmista että kirjautuminen on oikeaan tilaukseen

**4. "Application error" Azuressa**
- Odota 1-2 minuuttia, sovellus käynnistyy
- Tarkista lokit: `az webapp log tail`
- Varmista että `UseInMemoryDatabase` on `true`

**5. Swagger ei näy**
- Varmista että URL:ssa on `/swagger`
- Tyhjennä selaimen välimuisti
- Kokeile incognito/private-tilaa

### Hyödyllisiä komentoja:

```bash
# Listaa kaikki App Servicet
az webapp list --output table

# Näytä App Servicen URL
az webapp show --name NIMI --resource-group RG --query "defaultHostName" --output tsv

# Näytä käynnissä olevat prosessit
az webapp list-runtimes --output table

# Lataa deployment-loki
az webapp log download --name NIMI --resource-group RG
```

---

## Hyödyllisiä linkkejä

- [Azure CLI dokumentaatio](https://docs.microsoft.com/en-us/cli/azure/)
- [Azure App Service dokumentaatio](https://docs.microsoft.com/en-us/azure/app-service/)
- [ASP.NET Core deployment guide](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/azure-apps/)
- [Publish with Visual Studio](https://docs.microsoft.com/en-us/aspnet/core/tutorials/publish-to-azure-webapp-using-vs)

---


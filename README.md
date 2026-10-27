# GroupMate — prototips

Komandu komplektēšanas platformas **GroupMate** prototips. RTU kurss „Programmatūras izstrādes tehnoloģija”.

Izstrādātāji: Ņikita Platicins, Artjoms Sorokins, Kirills Bondarenko, Davids Jākobsons.

Sistēma no pieteikušos dalībnieku profiliem automātiski veido līdzsvarotas komandas. Katrai komandai tā aprēķina saderības novērtējumu (0–100) ar skaidrojumu. Organizators komandas var koriģēt un apstiprināt.

**Tehnoloģijas:** ASP.NET Core 8 (C#), Entity Framework Core, React + TypeScript, PostgreSQL, Docker, xUnit, GitHub Actions.

## Strādājošās sadaļas

Šajās sadaļās dati tiek nolasīti no PostgreSQL, apstrādāti un saglabāti:

1. **Dalībnieka profils:** prasmes ar līmeni 1–5, intereses, pieejamais laiks un vēlamā loma, kā arī datu eksports JSON formātā.
2. **Projekti un pieteikšanās:** projekta izveide (organizators), meklēšana un filtrēšana, pieteikšanās un pieteikuma atsaukšana (dalībnieks).
3. **Komandu saskaņošana:** komandu veidošana, novērtējums pa 4 kritērijiem, neiekļautie dalībnieki un prasmju trūkumu atskaite. Organizators var pārvietot dalībniekus (novērtējums tiek pārrēķināts) un apstiprināt komandas. Pēc apstiprināšanas dalībnieki saņem paziņojumus.

Tikai vizuāli (saskarnē atzīmēts „Tikai vizuāli”): pieslēgšanās ar paroli un SSO, reģistrācija, konta dzēšana, atsauksmes un administratora darbības.

**Atšķirības no dokumentācijas:**
- īstas autentifikācijas vietā tiek izvēlēts demonstrācijas lietotājs, bet lomu tiesības pārbauda serveris;
- e-pasta (SMTP) nosūtīšana tiek simulēta;
- Redis netiek izmantots.

## Palaišana

```bash
docker compose up --build
```

- Saskarne: http://localhost:3000
- Datubāzes pārlūks (Adminer): http://localhost:8081 — PostgreSQL, serveris `db`, lietotājs, parole un datubāze `groupmate`

Testi:

```bash
cd backend
dotnet test
```

## Demonstrācijas video

https://drive.google.com/file/d/1xs6zTSd_ylEluTGNTZXpVyPYYyyycN38/view?usp=sharing

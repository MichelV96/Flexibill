# Functioneel Ontwerp – Inkoopfactuurverwerkingsplatform voor MKB

**Versie:** 1.1 (compleet concept incl. rolafhankelijk dashboard)
**Datum:** 3 juli 2026
**Status:** Basispakket (incl. dashboard), Documentarchief, Declaratieverwerking en Inkoopmanagement volledig uitgewerkt in use cases en wireframes. Klaar voor bespreking met stakeholders en overdracht naar UX/UI en development.

---

## 1. Inleiding

Dit document beschrijft het functioneel ontwerp van een platform voor het verwerken van inkoopfacturen bij MKB-bedrijven. Het platform is modulair opgezet: klanten starten met een basispakket (CRM leveranciers + Factuurverwerking, inclusief fiatering en boekhoudkoppeling) en kunnen daarna losse modules toevoegen: Documentarchief, Declaratieverwerking en Inkoopmanagement.

Doelgroep: MKB-bedrijven die zelf hun administratie voeren.

## 2. Uitgangspunten en scope

- Het platform is een **multi-tenant SaaS-oplossing**.
- Basisstructuur: **Administratie → Vestiging(en) → Gebruiker(s)**.
- **CRM (leveranciers)** en **Factuurverwerking** (incl. fiatering en boekhoudkoppeling) vormen de **verplichte basis**.
- **Documentarchief**, **Declaratieverwerking** en **Inkoopmanagement** zijn losse, per administratie aan/uit te zetten modules (in deze prioriteitsvolgorde voor de bouw, zie hoofdstuk 11).
- Boekhoudkoppeling wordt ingesteld **per vestiging**, niet per administratie.
- Rollen zijn **combineerbaar** en worden altijd **expliciet** toegekend — geen enkele rol geeft automatisch rechten van een andere rol erbij.
- **Databeveiliging is vestiging-gebonden**: gebruikers zien alleen data van vestigingen waar ze toegang toe hebben (zie 3.5).
- Dit ontwerp beschrijft functionaliteit en gedrag, geen technische architectuur of database-ontwerp (het conceptuele domeinmodel in hoofdstuk 14 is een brug daarnaartoe).

## 3. Basisstructuur: Administratie, Vestiging, Gebruiker

### 3.1 Administratie
- De administratie is de juridische/fiscale eenheid (komt overeen met één KVK-inschrijving / bedrijf).
- Eén administratie heeft één of meerdere vestigingen en **N gebruikers**.
- Op administratieniveau worden ingesteld: actieve modules, huisstijl (logo op e-mails/exports), abonnementsvorm (zie hoofdstuk 12).
- Eén klant kan meerdere administraties hebben (bijv. een holding met meerdere BV's); deze zijn functioneel volledig gescheiden. Een gebruiker kan, indien uitgenodigd, toegang hebben tot meerdere administraties.

### 3.2 Vestiging
- Een vestiging is een fysieke of organisatorische locatie binnen een administratie (bijv. hoofdkantoor, filiaal, magazijn).
- **Boekhoudkoppeling wordt per vestiging geactiveerd en ingesteld.** Eén administratie kan dus vestigingen hebben die naar verschillende boekhoudpakketten exporteren.
- **Fiateringsflows worden per vestiging ingesteld** (zie 6.4) — niet alleen administratie-breed.
- Verder gebruikt voor: afwijkend afleveradres, eigen kostenplaats-toewijzing, eigen e-mail-inbox voor facturen (optioneel), rapportages per vestiging.
- Facturen en declaraties worden altijd aan één vestiging gekoppeld (verplicht veld), ook als een administratie maar één vestiging heeft.

### 3.3 Gebruiker
- Een gebruiker hoort bij één administratie en krijgt **toegang tot één of meerdere vestigingen** binnen die administratie. Deze toegang bepaalt welke data (leveranciers, facturen, declaraties, documenten) de gebruiker mag zien — zie 3.5.
- Een gebruiker kan **meerdere rollen tegelijk** hebben (zie 3.4), eventueel met een andere set rollen per vestiging (bijv. Fiatteerder voor vestiging A, maar geen rol voor vestiging B).

### 3.4 Rollen (simpel, combineerbaar, altijd expliciet toegekend)
| Rol | Omschrijving |
|---|---|
| Beheerder | Volledig beheer: gebruikers, instellingen, modules, boekhoudkoppelingen. Mag daarnaast facturen coderen die de OCR niet automatisch kon verwerken. |
| Leverancier lezer | Mag leveranciers inzien en **concept-leveranciers** aanmaken. |
| Leverancier beheerder | Mag leveranciers inzien, aanmaken, bewerken en concept-leveranciers activeren. Mag daarnaast facturen coderen die de OCR niet automatisch kon verwerken. |
| Fiatteerder | Fiatteert (keurt goed/af) facturen die aan hem/haar zijn toegewezen, binnen de vestiging(en) waar hij/zij toegang toe heeft. |
| Declarant *(module Declaratieverwerking)* | Mag declaraties indienen (voor zichzelf, of namens een medewerker als Beheerder/Declaratie-goedkeurder — zie 7.1). |
| Declaratie-goedkeurder *(module Declaratieverwerking)* | Mag declaraties goedkeuren/afkeuren, en namens een medewerker een declaratie indienen. |
| Documentlezer *(module Documentarchief)* | Mag documenten in het archief inzien. |
| Inkoop aanvrager *(module Inkoopmanagement)* | Mag inkoopaanvragen indienen. |
| Inkoop goedkeurder *(module Inkoopmanagement)* | Mag inkoopaanvragen/orders goedkeuren. |

> Belangrijk: **rollen geven nooit impliciet rechten van een andere rol.** Een Beheerder is dus niet automatisch ook Fiatteerder of Declaratie-goedkeurder — die rol moet er expliciet bij toegekend worden als iemand die taken ook mag uitvoeren.

### 3.5 Toegangsmodel: zichtbaarheid van data per vestiging
Dit is een kernregel die door het hele platform heen geldt:

- Elke gebruiker heeft toegang tot één of meerdere specifieke vestigingen.
- Overzichtspagina's (bijv. "alle leveranciers", "alle facturen") zijn **niet per vestiging apart**, maar tonen altijd **alle data waartoe de gebruiker toegang heeft**, gefilterd op basis van zijn/haar vestigingstoegang.
- **Regel voor gedeelde records** (een leverancier of factuur die aan meerdere vestigingen gekoppeld is): een gebruiker ziet het record zodra hij/zij toegang heeft tot **minstens één** van de gekoppelde vestigingen (unie, geen doorsnede).
  - *Voorbeeld:* leverancier X is gekoppeld aan vestiging 1 én 2. Een gebruiker met alleen toegang tot vestiging 1 ziet leverancier X. Een leverancier die uitsluitend aan vestiging 2 gekoppeld is, is voor die gebruiker niet zichtbaar.
- Dezelfde regel geldt voor facturen: een factuur hoort bij precies één vestiging, dus is alleen zichtbaar voor gebruikers met toegang tot die vestiging.
- **Fiateringsflows zijn per vestiging geconfigureerd** (zie 6.4). Bij het inrichten van een flow controleert het systeem of de toegewezen Fiatteerder(s) daadwerkelijk toegang hebben tot de betreffende vestiging, en toont een duidelijke **waarschuwing** als dat niet zo is (bijv. "Deze gebruiker heeft geen toegang tot vestiging X en kan hier dus niet fiatteren").

---

## 4. Algemene functionaliteiten (los van modules)

### 4.1 Registratie & onboarding
- Nieuwe klant registreert een administratie (bedrijfsgegevens, KVK, eerste vestiging, eerste Beheerder-account met e-mailadres).
- Verificatie van het account gebeurt via dezelfde OTP-inlogflow als hieronder beschreven (4.2).
- Onboarding-wizard: welke modules activeren, boekhoudpakket koppelen per vestiging, eerste leveranciers importeren (CSV) of handmatig invoeren.

### 4.2 Inloggen — wachtwoordloos via OTP
- Geen wachtwoorden. Gebruiker vult zijn e-mailadres in; het platform stuurt een **eenmalige code (OTP)** naar dat e-mailadres.
- Gebruiker voert de code in om in te loggen.
- Geen "onthoud mij"-functie — elke sessie vraagt opnieuw om een nieuwe code na verloop van de sessie.
- Code is kort geldig (**10 minuten**) en eenmalig bruikbaar, bestaat uit **6 cijfers**; na een aantal mislukte pogingen wordt een nieuwe code vereist.
- Rate limiting op het aanvragen van codes om misbruik/spam te voorkomen.
- Sessie verloopt na X minuten inactiviteit (instelbaar, met veilige default).

### 4.3 Gebruikersbeheer
- Beheerder nodigt gebruikers uit per e-mail, kent één of meerdere rollen en vestiging(en) toe.
- Gebruiker kan gedeactiveerd worden (i.p.v. verwijderd, i.v.m. audit trail/historie).
- Overzicht van laatste inlogmoment per gebruiker.

### 4.4 Notificaties
- In-app notificaties + e-mail (met voorkeursinstelling per gebruiker: alles, alleen belangrijk, uit).
- Voorbeelden: nieuwe factuur ter goedkeuring, factuur afgekeurd, declaratie ingediend, koppeling met boekhoudpakket mislukt, contract loopt binnenkort af, fiatteringsflow verwijst naar gebruiker zonder vestigingstoegang.

### 4.5 Instellingen per administratie
- Bedrijfsgegevens, logo/huisstijl, actieve modules en abonnementsvorm, e-mail-inbox-adres(sen) voor facturen. Boekhoudkoppeling, grootboekschema en fiateringsflows worden per vestiging ingesteld (zie 3.2, 6.4).

### 4.6 Audit trail / logging
- Wie heeft wat wanneer gewijzigd (factuur gecodeerd, goedgekeurd, leverancier aangepast, gebruiker toegevoegd, etc.). Read-only, niet verwijderbaar door reguliere gebruikers.

### 4.7 Taal & lokalisatie
- Voor nu **uitsluitend Nederlands**.

### 4.8 Help & support
- Help-widget/kennisbank binnen de applicatie, contactformulier/support-ticket.

---

## 5. Kernmodule A: CRM Leveranciers *(basis, altijd actief)*

### 5.1 Leveranciersbeheer
- Vastleggen: bedrijfsnaam, KVK-nummer, btw-nummer, IBAN(s), contactpersonen, adresgegevens, betaaltermijn, betaalwijze, categorie/branche, standaard grootboekrekening en kostenplaats (gebruikt als voorstel voor factuurregels, zie 6.3).
- Koppeling: welke vestiging(en) mogen met deze leverancier werken/facturen ontvangen (bepaalt ook zichtbaarheid, zie 3.5).
- Duplicate-detectie bij aanmaken (op KVK-nummer, IBAN of naam).

### 5.2 Concept-leveranciers
- Een gebruiker met rol **Leverancier lezer** kan een leverancier als **concept** aanmaken (bijv. bij het verwerken van een factuur van een nog onbekende leverancier).
- Een gebruiker met rol **Leverancier beheerder** (of Beheerder) beoordeelt en activeert het concept, en vult eventueel ontbrekende gegevens aan.
- **Een factuur van een concept-leverancier kan aangemaakt en gekoppeld worden, maar wordt niet verder in het proces gebracht (geen fiattering, geen verwerking naar boekhouding) totdat de leverancier is goedgekeurd/geactiveerd.** De factuur krijgt in de tussentijd de status "wacht op leverancier-goedkeuring" (zie 6.5).

### 5.3 Documenten & historie per leverancier
- Notities/logboek (bijv. telefoongesprekken, afspraken).
- Contracten en algemene voorwaarden: als module Documentarchief actief is, hier centraal opgeslagen en getoond bij de leverancier; anders eenvoudige lokale bijlage-opslag.
- Overzicht van alle facturen van deze leverancier (status, bedragen, historie).

---

## 6. Kernmodule B: Factuurverwerking *(basis, altijd actief — incl. fiatering en boekhoudkoppeling)*

### 6.1 Aanlevering van facturen
- Per administratie (en optioneel per vestiging) een uniek e-mailadres waar facturen naartoe gestuurd kunnen worden.
- Handmatige upload (PDF, UBL-XML, foto).
- Herkenning van dubbele facturen (zelfde leverancier + factuurnummer + bedrag).

### 6.2 Herkenning en matching (OCR)
- OCR/factuurherkenning haalt automatisch op: leverancier, factuurnummer, factuurdatum, vervaldatum, totaalbedragen (excl./incl. btw, btw-bedrag), IBAN, **en de afzonderlijke factuurregels** (omschrijving, aantal, eenheidsprijs, regelbedrag, btw-percentage per regel).
- Automatische matching aan bestaande leverancier in CRM; bij geen match: voorstel tot nieuwe (concept-)leverancier — zie 5.2 voor het vervolg daarvan.

### 6.3 Coderen / verwerken — per factuurregel
- **Codering gebeurt op regelniveau, niet op het totaal van de factuur.** Elke factuurregel krijgt zijn eigen grootboekrekening, kostenplaats/project en btw-code, zodat één factuur over meerdere grootboekrekeningen/kostenplaatsen verdeeld kan worden (bijv. een factuur met zowel kantoorartikelen als drukwerk).
- **Standaard doet de OCR dit automatisch per regel**: op basis van de regelomschrijving en eerdere codering van vergelijkbare regels bij dezelfde leverancier, met de standaard grootboekrekening/kostenplaats van de leverancier (5.1) als terugvaloptie.
- Gebruikers met rol **Beheerder** of **Leverancier beheerder** (met toegang tot de vestiging) kunnen de codering van elke regel altijd handmatig aanpassen, ook als de OCR deze al heeft ingevuld.
- **Als de OCR een regel niet automatisch kan coderen**, komt de factuur in de wachtrij "handmatig coderen"; de openstaande regel(s) zijn duidelijk gemarkeerd, de overige (al gecodeerde) regels blijven zichtbaar ter controle.
- **Validatie**: het systeem controleert dat de som van de regelbedragen (excl. en incl. btw) overeenkomt met de totaalbedragen op de factuur, en toont een waarschuwing bij een afwijking (bijv. bij afrondingsverschillen of een gemiste regel).

### 6.4 Fiatering (ingebouwd, per vestiging instelbaar — standaard eenvoudig, uitbreidbaar)
- **Fiateringsflows worden per vestiging ingericht** — elke vestiging heeft zijn eigen standaardflow.
- **Nieuwe vestiging**: krijgt bij aanmaak automatisch de simpele standaardflow (één Fiatteerder) toegewezen, zodat fiattering direct werkt. De Beheerder kan dit daarna per vestiging aanpassen/uitbreiden.
- **Standaard**: één Fiatteerder keurt een factuur goed of af — simpel en zonder extra configuratie nodig.
- **Uitbreidbaar**: de Beheerder kan per vestiging een uitgebreidere standaardflow instellen, bijvoorbeeld meerdere niveaus met bedragsgrenzen.
- **Uitzonderingen per leverancier**: binnen een vestiging kan per leverancier een afwijkende flow ingesteld worden die de standaardflow van die vestiging overschrijft, bijvoorbeeld altijd 2 Fiatteerders nodig waarvan 1 vaste persoon en 1 vrij te kiezen/roulerend, parallel of volgtijdelijk.
- **Toegangscontrole bij inrichten**: het systeem controleert of toegewezen Fiatteerders toegang hebben tot de vestiging van de flow, en waarschuwt duidelijk als dat niet het geval is (zie 3.5).
- **Vervanging/afwezigheid**: gebruiker kan een vervanger instellen; Beheerder kan een fiattering handmatig herroeren.
- **Herinneringen & escalatie**: automatische herinnering na X dagen; escalatie naar Beheerder bij overschrijding van een termijn.
- **Overzicht**: dashboard "openstaande fiatteringen" per gebruiker, en totaaloverzicht voor Beheerder. Bulk-goedkeuren mogelijk (met audit trail).

### 6.5 Statussen van een factuur
Nieuw → *(indien concept-leverancier: wacht op leverancier-goedkeuring)* → In behandeling/coderen → Ter goedkeuring → Goedgekeurd / Afgekeurd → Verwerkt (geëxporteerd) → Gearchiveerd.

### 6.6 Verwerking naar boekhoudsoftware (per vestiging)
- Ondersteunde pakketten: **Exact Online, AFAS, Visma.net, e-Boekhouden/Snelstart/Yuki, eAccounting**.
- Koppeling wordt **per vestiging** ingesteld en geauthenticeerd (OAuth waar van toepassing); verschillende vestigingen binnen dezelfde administratie kunnen dus met verschillende pakketten werken.
- Synchronisatie van stamdata **vanuit** het boekhoudpakket: grootboekrekeningen, kostenplaatsen/projecten, btw-codes, bestaande crediteuren (voor matching met CRM-leveranciers).
- Automatische export van goedgekeurde facturen als inkoopfactuur/boeking **met een boekingsregel per factuurregel** (elk met zijn eigen grootboekrekening, kostenplaats en btw-code), in plaats van één totaalregel. **Bij elke export wordt altijd het factuurdocument zelf, plus een rapport van de fiattering (wie heeft wanneer goedgekeurd), als bijlage bij de boeking meegestuurd** — ongeacht of de module Documentarchief actief is.
- Terugkoppeling van betaalstatus vanuit boekhoudpakket (indien API dit ondersteunt), zodat factuurstatus bijgewerkt wordt (bijv. "betaald").
- **Foutafhandeling**: exportlog met status per factuur, notificatie bij mislukte export of verbroken koppeling, handmatig opnieuw versturen na correctie.
- Zonder werkende koppeling (bijv. tijdens onboarding) blijft handmatige export (CSV/PDF) als vangnet beschikbaar.

---

## 7. Module: Declaratieverwerking

### 7.1 Indienen
- Gebruiker met rol **Declarant** dient declaratie in: bedrag, categorie (reiskosten, representatie, overig), datum, omschrijving, bonnetje/foto als bijlage, gekoppelde vestiging/project.
- **Een gebruiker met rol Beheerder of Declaratie-goedkeurder kan ook namens een medewerker een declaratie handmatig invoeren/uploaden** — bijvoorbeeld wanneer iemand een papieren bonnetje op het bureau legt. De declaratie wordt dan geregistreerd op naam van die medewerker.
- Optioneel: kilometerregistratie (rit van–naar, aantal km, automatische berekening tegen ingesteld tarief).
- Declaraties zijn een **pure netto-vergoeding** aan de medewerker; er wordt geen btw-technische verwerking (aftrek) op toegepast.

### 7.2 Goedkeuring
- Gebruiker met rol **Declaratie-goedkeurder** keurt declaraties goed/af.
- Declaraties gebruiken **dezelfde flexibele fiatteringslogica als facturen** (standaardflow per vestiging + eventuele uitzonderingen, bedragsgrenzen, meerdere niveaus) in plaats van een aparte, simpelere flow.

### 7.3 Verwerking
- Na goedkeuring: overzicht van te betalen declaraties per medewerker/periode.
- Met de boekhoudkoppeling (basisfunctionaliteit, zie 6.6): export als betaalopdracht/boeking naar het boekhoudpakket van de betreffende vestiging.

### 7.4 Mobiele app
- Voor deze module is een **mobiele app een must**, specifiek voor het **indienen van declaraties** (foto van bonnetje maken, gegevens invullen, versturen). Goedkeuren via mobiel is voor nu geen vereiste.

---

## 8. Module: Documentarchief

### 8.1 Centrale opslag
- Eén archief voor: facturen, contracten, declaratiebonnen, overige documenten (bijv. leveranciersovereenkomsten, correspondentie).
- Elk document krijgt metadata: type, gekoppelde leverancier/factuur/vestiging, uploaddatum, tags.

### 8.2 Zoeken
- Volledige tekst-zoekfunctie (op basis van OCR-inhoud), filteren op type/leverancier/periode/tag.

### 8.3 Bewaarbeleid
- **Standaardgedrag (module niet actief)**: een document (inkoopfactuur of declaratiebon) wordt **3 maanden bewaard nadat het succesvol is gesynchroniseerd naar het boekhoudpakket**, en daarna automatisch verwijderd. (Is er geen boekhoudkoppeling of synchronisatie mislukt, dan gaat de termijn pas lopen zodra dit alsnog lukt — het document wordt niet verwijderd voordat het veilig is overgekomen.) Let op: de kopie die als bijlage naar het boekhoudpakket is gestuurd (zie 6.6) blijft daar wél bestaan; dit is onafhankelijk van het platform.
- **Met module actief**: documenten worden voor de langere termijn bewaard, met als uitgangspunt de fiscale bewaarplicht van 7 jaar voor financiële documenten, instelbaar door de Beheerder.
- Automatische signalering vóór het verlopen van bewaartermijn (indien van toepassing) en vóór het verlopen van contracten.

### 8.4 Rechten
- Rol **Documentlezer**: inzien van documenten. Rechten op documentniveau/type verder instelbaar (bijv. sommige documenttypen alleen zichtbaar voor Beheerder).

---

## 9. Module: Inkoopmanagement *(volwaardig, los aan/uit te zetten per administratie)*

### 9.1 Inkoopaanvragen en -orders
- Gebruiker met rol **Inkoop aanvrager** kan een inkoopaanvraag indienen; na goedkeuring door een **Inkoop goedkeurder** wordt dit een inkooporder richting leverancier (uit CRM).

### 9.2 Budgetbewaking
- Budgetten instelbaar per kostenplaats/project/periode; systeem bewaakt verbruik t.o.v. budget bij nieuwe inkoopaanvragen/orders.

### 9.3 Contractbeheer
- Vastleggen leverancierscontracten: looptijd, opzegtermijn, waarde, gekoppelde documenten (via Documentarchief indien actief).
- Signalering bij naderende einddatum/opzegtermijn.

### 9.4 Drie-weg-matching
- Koppeling van inkooporder → ontvangst (afgevinkt door medewerker/magazijn) → binnenkomende factuur (uit Factuurverwerking), met automatische signalering bij afwijkingen in aantal/bedrag.

### 9.5 Leveranciersprestaties
- Eenvoudige rapportage vanuit CRM-data: gemiddelde levertijd, aantal afwijkingen, totale inkoopwaarde per leverancier/periode.

---

## 10. Rollen en rechten — overzicht (concept)

| Actie | Beheerder | Leverancier lezer | Leverancier beheerder | Fiatteerder | Declarant | Declaratie-goedkeurder | Documentlezer | Inkoop aanvrager | Inkoop goedkeurder |
|---|---|---|---|---|---|---|---|---|---|
| Gebruikers/modules/instellingen beheren | ✔ | – | – | – | – | – | – | – | – |
| Boekhoudkoppeling/fiateringsflow per vestiging instellen | ✔ | – | – | – | – | – | – | – | – |
| Leverancier inzien | ✔ | ✔ | ✔ | – | – | – | – | – | – |
| Concept-leverancier aanmaken | ✔ | ✔ | ✔ | – | – | – | – | – | – |
| Leverancier volledig aanmaken/bewerken/activeren | ✔ | – | ✔ | – | – | – | – | – | – |
| Factuur handmatig coderen (OCR-fallback) | ✔ | – | ✔ | – | – | – | – | – | – |
| Factuur fiatteren | – | – | – | ✔ | – | – | – | – | – |
| Declaratie indienen (ook namens medewerker) | – | – | – | – | ✔ | ✔ | – | – | – |
| Declaratie goedkeuren | – | – | – | – | – | ✔ | – | – | – |
| Documentarchief inzien | – | – | – | – | – | – | ✔ | – | – |
| Inkoopaanvraag indienen | – | – | – | – | – | – | – | ✔ | – |
| Inkoopaanvraag/order goedkeuren | – | – | – | – | – | – | – | – | ✔ |

*Beheerder heeft geen impliciete fiatterings-, declaratiegoedkeurings- of inkoopgoedkeuringsrechten — deze moeten er als losse rol expliciet bij toegekend worden.*

---

## 11. Module-architectuur en activering

- Elke administratie heeft **CRM Leveranciers** en **Factuurverwerking** (incl. fiatering en boekhoudkoppeling) standaard actief — dit is geen los te koppelen module.
- Losse modules, per administratie aan/uit te zetten, in **bouwprioriteit**:
  1. **Documentarchief**
  2. **Declaratieverwerking**
  3. **Inkoopmanagement**
- Afhankelijkheden:
  - **Inkoopmanagement** (drie-weg-matching) heeft Factuurverwerking nodig (altijd aanwezig); werkt rijker in combinatie met Documentarchief (contracten) maar is daar niet van afhankelijk.
  - **Declaratieverwerking** hergebruikt de fiatteringslogica uit de basis (zie 7.2); kan los van Documentarchief en Inkoopmanagement functioneren.
  - **Documentarchief** kan los werken, maar wordt functioneel rijker in combinatie met CRM, Factuurverwerking en Inkoopmanagement.

---

## 12. Abonnementsvormen

- Twee prijsmodellen naast elkaar:
  1. **Gebruiksgebaseerd** (bijv. per verwerkte factuur/declaratie) — lage instapdrempel, geschikt voor lichte gebruikers.
  2. **Vaste prijs** (bijv. per maand, evt. met module-toeslagen) — voordeliger bij hoog gebruik.
- Functionele impact:
  - De administratie-instellingen tonen de gekozen abonnementsvorm en (indien gebruiksgebaseerd) het actuele verbruik in de huidige periode.
  - Er is **gebruiksregistratie** nodig (tellen van verwerkte facturen/declaraties per administratie) als basis voor facturatie, ongeacht gekozen model.
  - Overstappen tussen prijsmodellen moet mogelijk zijn vanuit de instellingenpagina (exacte voorwaarden/moment zijn een commerciële, geen functionele, keuze).

---

## 13. Niet-functionele eisen

- **Beveiliging & AVG:** encryptie van data (in rust en onderweg), rolgebaseerde en vestiging-gebaseerde toegang (zie 3.5), verwerkersovereenkomst richting klanten, recht op inzage/verwijdering van persoonsgegevens.
- **Authenticatie:** wachtwoordloos via OTP per e-mail; rate limiting en beperkt aantal pogingen ter voorkoming van misbruik.
- **Multi-tenancy:** volledige scheiding van data tussen administraties.
- **Beschikbaarheid:** streefcijfer uptime (te bepalen, bijv. 99,5%).
- **Auditability:** alle wijzigingen in facturen, goedkeuringen en instellingen worden gelogd en zijn niet aanpasbaar achteraf.
- **Taal:** v1 uitsluitend Nederlands.
- **Gebruiksregistratie:** meten van verwerkte facturen/declaraties per administratie t.b.v. gebruiksgebaseerde abonnementen.
- **Performance:** OCR-verwerking van een factuur binnen enkele seconden tot maximaal een minuut na aanlevering (te bevestigen als KPI).
- **Mobiel:** native (of vergelijkbare) mobiele app voor het indienen van declaraties, met camera-toegang voor bonnetjes.
- **Bewaartermijnen:** automatische verwijdering van inkoopfactuurdocumenten na 3 maanden wanneer Documentarchief niet actief is (zie 8.3); export naar boekhoudpakket blijft daarbij altijd los bewaard bij de boeking (zie 6.6).

---

## 14. Domeinschets (conceptueel datamodel)

Zie het visuele schema hierboven in de chat. De belangrijkste relaties:

- **Administratie** 1—n **Vestiging**
- **Vestiging** n—n **Gebruiker** (bepaalt databeleid/zichtbaarheid, zie 3.5)
- **Gebruiker** n—n **Rol** (combineerbaar, altijd expliciet)
- **Vestiging** 1—0..1 **Boekhoudkoppeling**; **Vestiging** 1—n **Fiateringsflow** (standaard + uitzonderingen per leverancier)
- **Administratie** 1—n **Leverancier**; **Leverancier** n—n **Vestiging**
- **Leverancier** 1—n **Factuur**; **Factuur** n—1 **Vestiging**
- **Factuur** 1—n **Factuurregel** (elk met eigen grootboekrekening, kostenplaats en btw-code)
- **Factuur** 1—n **Fiatteringsstap**
- *(module)* **Gebruiker** 1—n **Declaratie** (ook aangemaakt namens een andere gebruiker); **Declaratie** n—1 **Vestiging**
- *(module)* **Document** n—0..1 **Factuur / Leverancier / Contract**
- *(module)* **Leverancier** 1—n **Inkooporder**; **Inkooporder** n—1 **Vestiging**; **Inkooporder** 1—n **Factuur**; **Leverancier** 1—n **Contract**

---

## 15. Aannames en openstaande vragen

Alle eerdere openstaande vragen zijn beantwoord en verwerkt in dit document (v0.3):
- Nieuwe vestigingen krijgen automatisch de simpele standaard-fiateringsflow (zie 6.4).
- De bewaartermijn van 3 maanden (zonder Documentarchief-module) gaat in nadat een document is gesynchroniseerd naar het boekhoudpakket, en geldt voor zowel facturen als declaratiebonnen (zie 8.3).
- OTP-specificaties: 6 cijfers, 10 minuten geldig (zie 4.2).

Er zijn op dit moment geen openstaande vragen meer. Nieuwe vragen die tijdens de verdere uitwerking (use cases, schermontwerpen) naar boven komen, worden in een volgende versie van dit document toegevoegd.

---

## 16. Use cases — CRM Leveranciers & Factuurverwerking

Onderstaand de belangrijkste use cases van de twee basismodules. Wireframes van de belangrijkste schermen staan als losse visuals bij dit gesprek (Facturenoverzicht, Factuurverwerkingsscherm, Leveranciersoverzicht, Fiattering).

### 16.1 CRM Leveranciers

**UC-C1: Leverancier volledig aanmaken**
- Actor: Leverancier beheerder, Beheerder
- Trigger: Gebruiker klikt "Nieuwe leverancier" op het leveranciersoverzicht.
- Voorwaarden: Gebruiker heeft rol Leverancier beheerder of Beheerder.
- Hoofdscenario:
  1. Gebruiker vult bedrijfsgegevens in (naam, KVK, btw-nummer, IBAN, adres, contactpersoon).
  2. Gebruiker kiest betaaltermijn, categorie en standaard grootboekrekening/kostenplaats.
  3. Gebruiker koppelt de leverancier aan één of meerdere vestigingen (alleen vestigingen waar hij/zij zelf toegang toe heeft, zie 3.5).
  4. Systeem controleert op duplicaten (KVK/IBAN/naam) en toont waarschuwing bij mogelijke match.
  5. Gebruiker bevestigt; leverancier krijgt status "Actief".
- Uitzonderingen:
  - Duplicaat gevonden → gebruiker kan doorgaan (bewust) of annuleren en bestaande leverancier openen.
  - Verplicht veld ontbreekt → validatiemelding, opslaan wordt geblokkeerd.

**UC-C2: Concept-leverancier aanmaken**
- Actor: Leverancier lezer (ook door Leverancier beheerder/Beheerder te gebruiken)
- Trigger: Bij het verwerken van een factuur van een onbekende leverancier, of handmatig vanuit het leveranciersoverzicht.
- Hoofdscenario:
  1. Gebruiker vult minimaal de basisgegevens in (naam, en indien bekend KVK/IBAN).
  2. Systeem slaat de leverancier op met status "Concept".
  3. Leverancier verschijnt in het overzicht met een duidelijk concept-label, zichtbaar voor Beheerder en Leverancier beheerder.
- Uitzonderingen: Duplicaatcontrole loopt ook hier; bij match wordt voorgesteld de bestaande (concept- of actieve) leverancier te gebruiken.

**UC-C3: Concept-leverancier beoordelen en activeren**
- Actor: Leverancier beheerder, Beheerder
- Trigger: Gebruiker opent een leverancier met status "Concept" (bijv. vanuit een notificatie of het overzicht).
- Hoofdscenario:
  1. Gebruiker controleert/vult ontbrekende gegevens aan (KVK, IBAN, betaaltermijn, grootboekrekening).
  2. Gebruiker activeert de leverancier.
  3. Alle facturen die op deze leverancier "wachten op leverancier-goedkeuring" stromen automatisch door naar de eerstvolgende processtap (coderen/fiatteren, zie UC-F7).
- Uitzonderingen: Gebruiker kan de concept-leverancier ook afwijzen/verwijderen; gekoppelde facturen blijven dan in een "geblokkeerd"-status staan totdat een gebruiker de koppeling herstelt.

**UC-C4: Leverancier bewerken**
- Actor: Leverancier beheerder, Beheerder
- Hoofdscenario: Gebruiker opent leverancier, wijzigt gegevens, slaat op. Wijziging komt in de audit trail (4.6).

**UC-C5: Leveranciers zoeken en inzien**
- Actor: Alle rollen met leverancier-toegang (Leverancier lezer/beheerder, Beheerder)
- Hoofdscenario:
  1. Gebruiker opent leveranciersoverzicht.
  2. Systeem toont automatisch alleen leveranciers gekoppeld aan minstens één vestiging waar de gebruiker toegang toe heeft (zie 3.5).
  3. Gebruiker kan zoeken op naam/KVK en filteren op categorie, status (concept/actief) of vestiging.

**UC-C6: Signalering leveranciers met ontbrekende gegevens**
- Actor: Systeem, Leverancier beheerder, Beheerder
- Trigger: Doorlopend, en bij elke wijziging aan een leverancier.
- Hoofdscenario: Het systeem controleert actieve leveranciers op ontbrekende gegevens die nodig zijn voor een correcte verwerking (bijv. IBAN, KVK-nummer, standaard grootboekrekening). Leveranciers met hiaten worden gemarkeerd in het leveranciersoverzicht en verschijnen als aandachtspunt op het dashboard (zie hoofdstuk 20).
- Toelichting: Dit is nadrukkelijk iets anders dan een concept-leverancier (zie 5.2) — een actieve leverancier met ontbrekende gegevens blijft gewoon bruikbaar, dit is puur een signalering om de gegevens op termijn compleet te maken.

### 16.2 Factuurverwerking

**UC-F1: Factuur binnenkomen en automatisch verwerken**
- Actor: Systeem (OCR), met Beheerder/Leverancier beheerder als terugvalmogelijkheid
- Trigger: Factuur komt binnen per e-mail of wordt handmatig geüpload.
- Hoofdscenario:
  1. Systeem herkent het document (OCR): leverancier, factuurnummer, datum, totaalbedragen, IBAN, en de afzonderlijke factuurregels.
  2. Systeem matcht automatisch aan een bestaande leverancier in de CRM.
  3. Systeem stelt per factuurregel een grootboekrekening, kostenplaats en btw-code voor/vult deze in, op basis van de regelomschrijving en eerdere codering van vergelijkbare regels.
  4. Systeem controleert of de som van de regelbedragen overeenkomt met de totaalbedragen op de factuur.
  5. Systeem controleert op dubbele facturen (zelfde leverancier + factuurnummer + bedrag).
  6. Factuur krijgt status "Ter goedkeuring" en wordt aangeboden aan de juiste Fiatteerder(s) volgens de fiateringsflow van de vestiging (zie UC-F3/UC-F4).
- Uitzonderingen:
  - Geen leverancier-match → voorstel tot concept-leverancier (UC-C2); factuur krijgt status "Wacht op leverancier-goedkeuring" (UC-F7).
  - OCR kan één of meer regels niet automatisch coderen → die regels gaan naar de wachtrij "Handmatig coderen" (UC-F2), overige regels blijven gewoon zichtbaar.
  - Som van de regelbedragen wijkt af van het totaalbedrag → waarschuwing, factuur blijft in coderen totdat dit is opgelost.
  - Dubbele factuur gedetecteerd → melding aan gebruiker, factuur wordt gemarkeerd als mogelijk duplicaat i.p.v. automatisch doorgezet.

**UC-F2: Factuur handmatig coderen (OCR-fallback)**
- Actor: Beheerder, Leverancier beheerder (met toegang tot de vestiging van de factuur)
- Trigger: Eén of meer factuurregels staan in de wachtrij "Handmatig coderen".
- Hoofdscenario:
  1. Gebruiker opent de factuur, ziet document-preview naast de regels (deels automatisch ingevuld, deels leeg).
  2. Gebruiker vult/corrigeert per regel de grootboekrekening, kostenplaats en btw-code.
  3. Systeem controleert dat de som van de regelbedragen overeenkomt met het totaalbedrag op de factuur.
  4. Gebruiker bevestigt; factuur gaat door naar fiattering.

**UC-F3: Factuur fiatteren (standaardflow)**
- Actor: Fiatteerder
- Trigger: Factuur staat "Ter goedkeuring" en gebruiker is aangewezen Fiatteerder voor de vestiging.
- Hoofdscenario:
  1. Fiatteerder ziet de factuur in zijn/haar lijst "Openstaande fiatteringen".
  2. Fiatteerder bekijkt documentpreview en coderinggegevens.
  3. Fiatteerder keurt goed → factuur krijgt status "Goedgekeurd" en gaat door naar export (UC-F8).
- Uitzonderingen: Fiatteerder keurt af (UC-F5).

**UC-F4: Factuur fiatteren via uitzonderingsflow (meerdere fiatteerders)**
- Actor: Meerdere Fiatteerders (bijv. 1 vaste + 1 roulerend)
- Trigger: Factuur van een leverancier met een ingerichte uitzonderingsflow.
- Hoofdscenario:
  1. Systeem bepaalt op basis van de leverancier-uitzondering welke Fiatteerders nodig zijn en in welke volgorde (parallel of volgtijdelijk).
  2. Elke vereiste Fiatteerder keurt goed.
  3. Zodra alle vereiste goedkeuringen binnen zijn, krijgt de factuur status "Goedgekeurd".
- Uitzonderingen: Eén van de Fiatteerders keurt af → hele factuur krijgt status "Afgekeurd", ongeacht eventuele eerdere goedkeuringen.

**UC-F5: Factuur afkeuren**
- Actor: Fiatteerder
- Hoofdscenario: Fiatteerder keurt af en geeft verplicht een reden op. Factuur krijgt status "Afgekeurd"; indiener/coderende gebruiker krijgt notificatie en kan factuur opnieuw coderen en opnieuw indienen.

**UC-F6: Fiattering laten overnemen (afwezigheid)**
- Actor: Fiatteerder, Beheerder
- Hoofdscenario: Gebruiker stelt vooraf een vervanger in voor een periode; openstaande en nieuwe fiatteringen gaan in die periode naar de vervanger. Beheerder kan daarnaast altijd een individuele fiattering handmatig naar een andere gebruiker herroeren.

**UC-F7: Factuur van concept-leverancier**
- Actor: Systeem, Leverancier beheerder/Beheerder
- Hoofdscenario: Factuur krijgt status "Wacht op leverancier-goedkeuring" zodra deze gekoppeld is aan een concept-leverancier. Zodra de leverancier geactiveerd is (UC-C3), stroomt de factuur automatisch door naar coderen/fiatteren.

**UC-F8: Automatische export naar boekhoudpakket**
- Actor: Systeem
- Trigger: Factuur heeft status "Goedgekeurd".
- Hoofdscenario:
  1. Systeem stuurt de factuur als boeking naar het boekhoudpakket van de betreffende vestiging.
  2. Systeem voegt het factuurdocument én een fiatteringsrapport toe als bijlage.
  3. Factuur krijgt status "Verwerkt"; bij ontvangst van betaalstatus vanuit het boekhoudpakket wordt dit bijgewerkt.
- Uitzonderingen: Export mislukt (UC-F9).

**UC-F9: Mislukte export herstellen**
- Actor: Beheerder
- Trigger: Exportlog toont een mislukte export (bijv. verbroken koppeling, ontbrekende grootboekrekening).
- Hoofdscenario: Beheerder bekijkt foutmelding, corrigeert de oorzaak (bijv. koppeling herstellen, grootboekrekening aanpassen) en verstuurt de export opnieuw.

### 16.3 Beheer en inloggen

**UC-L1: Inloggen via OTP**
- Actor: Elke gebruiker
- Trigger: Gebruiker opent het platform en is niet ingelogd.
- Hoofdscenario:
  1. Gebruiker vult e-mailadres in.
  2. Systeem stuurt een 6-cijferige code naar dat e-mailadres (geldig 10 minuten).
  3. Gebruiker voert de code in.
  4. Systeem valideert de code en start de sessie.
- Uitzonderingen:
  - Onbekend e-mailadres → generieke melding ("als dit adres bekend is, ontvang je een code"), om te voorkomen dat bestaande accounts uitgelekt worden.
  - Verkeerde/verlopen code → foutmelding met mogelijkheid een nieuwe code aan te vragen.
  - Te veel mislukte pogingen → tijdelijke blokkade van nieuwe codes voor dat e-mailadres.

**UC-B1: Gebruiker uitnodigen**
- Actor: Beheerder
- Hoofdscenario:
  1. Beheerder vult het e-mailadres van de nieuwe gebruiker in.
  2. Beheerder kent één of meerdere rollen en één of meerdere vestigingen toe.
  3. Systeem verstuurt een uitnodiging; gebruiker activeert het account via OTP-inlog (UC-L1).
- Uitzonderingen: E-mailadres al in gebruik binnen de administratie → melding, geen dubbele uitnodiging mogelijk.

**UC-B2: Vestiging aanmaken**
- Actor: Beheerder
- Hoofdscenario: Beheerder vult naam en adres van de vestiging in. De vestiging krijgt automatisch de simpele standaard-fiateringsflow (zie 6.4) en kan direct gekoppeld worden aan een boekhoudpakket.

**UC-B3: Boekhoudkoppeling instellen per vestiging**
- Actor: Beheerder
- Hoofdscenario:
  1. Beheerder kiest het boekhoudpakket voor de vestiging.
  2. Beheerder rondt de authenticatie af (OAuth/inloggegevens, afhankelijk van pakket).
  3. Systeem synchroniseert stamdata (grootboekrekeningen, kostenplaatsen, btw-codes, crediteuren).
- Uitzonderingen: Authenticatie mislukt → foutmelding met mogelijkheid opnieuw te proberen.

**UC-B4: Standaard fiateringsflow instellen per vestiging**
- Actor: Beheerder
- Hoofdscenario:
  1. Beheerder opent de fiateringsinstellingen van een vestiging.
  2. Beheerder stelt niveau(s) in (bijv. 1 Fiatteerder, of meerdere niveaus met bedragsgrenzen) en wijst gebruikers toe.
  3. Systeem controleert of de toegewezen gebruikers toegang hebben tot de vestiging en waarschuwt zo niet (zie 3.5).

**UC-B5: Uitzonderingsflow per leverancier instellen**
- Actor: Beheerder
- Hoofdscenario:
  1. Beheerder kiest een leverancier binnen een vestiging.
  2. Beheerder stelt een afwijkende flow in (aantal Fiatteerders, vast/roulerend, parallel/volgtijdelijk, eventueel bedragsgrens).
  3. Nieuwe facturen van deze leverancier binnen deze vestiging gebruiken vanaf nu de uitzonderingsflow in plaats van de standaardflow.

**UC-B6: Module activeren/deactiveren**
- Actor: Beheerder
- Hoofdscenario: Beheerder schakelt een module (Documentarchief, Declaratieverwerking, Inkoopmanagement) aan/uit vanuit de instellingenpagina; bijbehorende rollen en menu-items verschijnen/verdwijnen direct.

---

## 17. Use cases — Documentarchief

Wireframes van de belangrijkste schermen staan als losse visuals bij dit gesprek (Documentenoverzicht, Documentdetail, Bewaarbeleid instellen).

**UC-D1: Document zoeken en filteren**
- Actor: Documentlezer, Beheerder, Leverancier beheerder
- Hoofdscenario:
  1. Gebruiker opent het documentarchief.
  2. Systeem toont alleen documenten van vestigingen waartoe de gebruiker toegang heeft (zie 3.5).
  3. Gebruiker filtert op type (factuur/contract/overig), leverancier, periode of tag, en/of zoekt op inhoud (volledige tekst, op basis van OCR).

**UC-D2: Document inzien**
- Actor: Documentlezer, Beheerder, Leverancier beheerder
- Hoofdscenario: Gebruiker opent een document; systeem toont preview, metadata (type, vestiging, uploaddatum, tags) en eventuele koppelingen (bijv. bijbehorende factuur of leverancier).

**UC-D3: Bewaarbeleid en rechten instellen**
- Actor: Beheerder
- Trigger: Module Documentarchief is geactiveerd.
- Hoofdscenario:
  1. Beheerder stelt per documenttype een bewaartermijn in (uitgangspunt: 7 jaar voor financiële documenten).
  2. Beheerder bepaalt welke documenttypen alleen voor Beheerder zichtbaar zijn versus voor elke Documentlezer.
- Uitzonderingen: Bewaartermijn korter dan de fiscale bewaarplicht van 7 jaar voor financiële documenten → waarschuwing, geen harde blokkade (verantwoordelijkheid van de klant).

**UC-D4: Automatische signalering bij verlopende termijn**
- Actor: Systeem, Beheerder
- Hoofdscenario: Systeem signaleert (notificatie, zie 4.4) vóór het verlopen van een bewaartermijn of een gekoppeld contract, zodat tijdig actie ondernomen kan worden.

---

## 18. Use cases — Declaratieverwerking

Wireframes van de belangrijkste schermen staan als losse visuals bij dit gesprek (mobiele indien-flow, declaratieoverzicht, goedkeuren, namens medewerker invoeren).

**UC-DC1: Declaratie indienen (declarant)**
- Actor: Declarant
- Trigger: Medewerker heeft een uitgave gedaan en wil deze vergoed krijgen.
- Hoofdscenario:
  1. Gebruiker opent "Nieuwe declaratie" (via de mobiele app of de webapplicatie).
  2. Gebruiker kiest het type: bonnetje/uitgave of kilometerregistratie.
  3. **Bij bonnetje**: gebruiker maakt een foto of uploadt het bonnetje, en vult bedrag, categorie, datum, omschrijving en vestiging/project in.
  4. **Bij kilometers**: gebruiker vult vertrek- en aankomstlocatie, aantal kilometers (of laat dit automatisch berekenen) en het ritdoel in; het systeem berekent de vergoeding op basis van het ingestelde kilometertarief.
  5. Gebruiker verstuurt de declaratie; deze gaat naar goedkeuring volgens de fiateringsflow van de vestiging (zie UC-DC3).
- Uitzonderingen: Verplichte bijlage ontbreekt bij een bonnetje → validatiemelding, versturen wordt geblokkeerd.

**UC-DC2: Declaratie namens medewerker invoeren**
- Actor: Beheerder, Declaratie-goedkeurder
- Trigger: Een medewerker legt een papieren bonnetje op het bureau of stuurt het per e-mail.
- Hoofdscenario:
  1. Gebruiker opent "Nieuwe declaratie" en kiest "Namens een medewerker".
  2. Gebruiker selecteert de medewerker uit de gebruikerslijst.
  3. Gebruiker vult de declaratie verder in zoals in UC-DC1.
  4. De declaratie wordt geregistreerd op naam van de gekozen medewerker en doorloopt daarna hetzelfde proces.

**UC-DC3: Declaratie goedkeuren/afkeuren**
- Actor: Declaratie-goedkeurder
- Hoofdscenario: Verloopt op dezelfde manier als bij facturen (zie UC-F3/UC-F4/UC-F5): een standaardflow per vestiging, met eventuele uitzonderingen en meerdere niveaus met bedragsgrenzen. Bij afkeuring is een reden verplicht, zichtbaar voor de indiener.

**UC-DC4: Verwerking naar boekhoudpakket**
- Actor: Systeem
- Trigger: Declaratie is goedgekeurd.
- Hoofdscenario: Systeem stuurt de declaratie als betaalopdracht/boeking naar het boekhoudpakket van de betreffende vestiging, met het bonnetje/de foto als bijlage.

**UC-DC5: Declaraties overzicht inzien**
- Actor: Declarant (eigen declaraties), Declaratie-goedkeurder (declaraties binnen toegankelijke vestigingen, zie 3.5), Beheerder
- Hoofdscenario: Gebruiker filtert het overzicht op status, periode, medewerker of vestiging.

---

## 19. Use cases — Inkoopmanagement

Wireframes van de belangrijkste schermen staan als losse visuals bij dit gesprek (inkoopaanvraag indienen, goedkeuren, budgetbewaking, contractbeheer, drie-weg-matching).

**UC-I1: Inkoopaanvraag indienen**
- Actor: Inkoop aanvrager
- Trigger: Medewerker wil iets inkopen.
- Hoofdscenario:
  1. Gebruiker kiest een leverancier uit de CRM (of vult een nieuwe leverancier als vrije tekst in).
  2. Gebruiker specificeert de aan te schaffen artikelen/diensten (omschrijving, aantal, geschatte prijs per stuk).
  3. Gebruiker kiest kostenplaats/project en vestiging.
  4. Systeem toont het resterende budget voor die kostenplaats/periode.
  5. Gebruiker dient de aanvraag in.
- Uitzonderingen: Aanvraag overschrijdt het beschikbare budget → duidelijke waarschuwing; de aanvraag kan alsnog ingediend worden ter beoordeling door de Inkoop goedkeurder.

**UC-I2: Inkoopaanvraag goedkeuren en omzetten naar inkooporder**
- Actor: Inkoop goedkeurder
- Hoofdscenario:
  1. Gebruiker bekijkt de openstaande aanvraag, inclusief de budgetcontext.
  2. Gebruiker keurt goed → de aanvraag wordt een inkooporder richting de leverancier; het bedrag wordt in mindering gebracht op het beschikbare budget.
- Uitzonderingen: Gebruiker keurt af met een reden; de aanvrager kan de aanvraag aanpassen en opnieuw indienen.

**UC-I3: Budget bewaken**
- Actor: Systeem, Beheerder (instellen), Inkoop goedkeurder (raadplegen)
- Hoofdscenario: Beheerder stelt budgetten in per kostenplaats/project/periode. Het systeem toont bij elke nieuwe aanvraag, en in een apart overzicht, het verbruik ten opzichte van het budget, en waarschuwt bij (dreigende) overschrijding.

**UC-I4: Contract vastleggen en bewaken**
- Actor: Beheerder, Inkoop goedkeurder
- Hoofdscenario:
  1. Gebruiker legt een leverancierscontract vast: leverancier, looptijd, opzegtermijn, waarde, gekoppeld document (via Documentarchief indien actief).
  2. Systeem signaleert automatisch vóór het verlopen van de opzegtermijn/einddatum (zie 4.4).

**UC-I5: Ontvangst registreren**
- Actor: Inkoop aanvrager (of gemachtigde medewerker/magazijn)
- Trigger: Bestelde artikelen/diensten zijn binnengekomen.
- Hoofdscenario: Gebruiker vinkt de inkooporder (deels) af als ontvangen, met aantal en datum.

**UC-I6: Drie-weg-matching**
- Actor: Systeem, Beheerder/Inkoop goedkeurder
- Trigger: Een factuur komt binnen die gekoppeld is aan een inkooporder.
- Hoofdscenario:
  1. Systeem vergelijkt automatisch de inkooporder, de geregistreerde ontvangst en de binnengekomen factuur (aantallen en bedragen).
  2. Bij overeenstemming kan de factuur normaal verder door de fiatteringsflow.
- Uitzonderingen: Bij afwijking markeert het systeem de factuur duidelijk (bijv. "meer gefactureerd dan ontvangen") en informeert de betrokken Fiatteerder/Inkoop goedkeurder, vóórdat verdere verwerking plaatsvindt.

**UC-I7: Leveranciersprestaties inzien**
- Actor: Beheerder, Inkoop goedkeurder
- Hoofdscenario: Gebruiker bekijkt een rapportage per leverancier: gemiddelde levertijd, aantal afwijkingen bij matching, totale inkoopwaarde per periode.

---

## 20. Dashboard (startscherm na inloggen)

Wireframes van twee dashboardvarianten staan als losse visuals bij dit gesprek: één voor een Beheerder (breed), één voor een gebruiker met alleen de rol Fiatteerder (smal) — ter illustratie van hoe het dashboard meebeweegt met de toegekende rollen.

**UC-H1: Dashboard tonen na inloggen**
- Actor: Elke gebruiker
- Trigger: Gebruiker logt in (UC-L1).
- Hoofdscenario:
  1. Systeem toont een dashboard opgebouwd uit kaarten/widgets.
  2. Welke kaarten zichtbaar zijn, hangt af van de rol(len) van de gebruiker én de actieve modules van de administratie — een gebruiker ziet nooit een kaart voor een module die niet actief is, of voor een rol die hij/zij niet heeft.
  3. Elke kaart toont een korte samenvatting (bijv. een aantal) met een link door naar het volledige overzicht.
- Rolafhankelijke kaarten:

| Rol | Dashboardkaart |
|---|---|
| Beheerder | Mislukte exports naar boekhoudpakket, leveranciers met ontbrekende gegevens (UC-C6), concept-leveranciers te beoordelen, uitgenodigde gebruikers die nog niet zijn geactiveerd, kort overzicht actieve modules |
| Leverancier lezer / Leverancier beheerder | Concept-leveranciers te beoordelen, leveranciers met ontbrekende gegevens |
| Fiatteerder | Aantal openstaande fiatteringen (facturen), met eventuele waarschuwing bij items die dreigen te escaleren (zie 6.4) |
| Declarant | Status van eigen recent ingediende declaraties |
| Declaratie-goedkeurder | Aantal openstaande declaraties ter goedkeuring |
| Inkoop aanvrager | Status van eigen inkoopaanvragen, resterend budget van de eigen kostenplaats |
| Inkoop goedkeurder | Aantal openstaande inkoopaanvragen, contracten met naderende opzegtermijn |

- Uitzonderingen: Een gebruiker zonder enige operationele rol (zeldzaam, bijv. alleen Documentlezer) ziet een minimaal dashboard met alleen algemene informatie en een link naar het documentarchief.

---

*Dit document is een volledig concept van het functioneel ontwerp: basispakket (CRM, Factuurverwerking, fiattering, beheer, inloggen, dashboard) en alle drie de modules (Documentarchief, Declaratieverwerking, Inkoopmanagement) zijn uitgewerkt in use cases en wireframes. Volgende stap: dit concept bespreken met stakeholders en overdragen aan UX/UI-ontwerp en development voor verdere detaillering.*

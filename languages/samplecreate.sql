CREATE TABLE LanguageCodes (
   LangID      char(3) NOT NULL,  -- Three-letter code
   CountryID   char(2) NOT NULL,  -- Main country where used
   LangStatus  char(1) NOT NULL,  -- L(iving), (e)X(tinct)
   Name    varchar(75) NOT NULL)  -- Primary name in that country

CREATE TABLE CountryCodes (
   CountryID  char(2) NOT NULL,  -- Two-letter code from ISO3166
   Name   varchar(75) NOT NULL,  -- Country name
   Area   varchar(10) NOT NULL ) -- World area 
 
CREATE TABLE LanguageIndex (
   LangID    char(3) NOT NULL,  -- Three-letter code for language
   CountryID char(2) NOT NULL,  -- Country where this name is used
   NameType  char(2) NOT NULL,  -- L(anguage), LA(lternate),
                                -- D(ialect), DA(lternate)
                                -- LP,DP (a pejorative alternate)
   Name  varchar(75) NOT NULL ) -- The name

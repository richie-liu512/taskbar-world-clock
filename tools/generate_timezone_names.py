import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TaskbarWorldClock.cs"
CLDR = ROOT / "dist" / "cldr-tz"

LOCALE_FILES = {
    "zh-CN": ("zh-Hans", "zh-Hans-timeZoneNames.json"),
    "zh-TW": ("zh-Hant", "zh-Hant-timeZoneNames.json"),
    "en-US": ("en", "en-timeZoneNames.json"),
    "ja-JP": ("ja", "ja-timeZoneNames.json"),
    "ko-KR": ("ko", "ko-timeZoneNames.json"),
    "de-DE": ("de", "de-timeZoneNames.json"),
    "fr-FR": ("fr", "fr-timeZoneNames.json"),
    "es-ES": ("es", "es-timeZoneNames.json"),
    "pt-BR": ("pt", "pt-timeZoneNames.json"),
    "ru-RU": ("ru", "ru-timeZoneNames.json"),
}

FIXED_ZONE_NAMES = {
    "Dateline Standard Time": "UTC-12:00",
    "UTC-11": "UTC-11:00",
    "UTC-09": "UTC-09:00",
    "UTC-08": "UTC-08:00",
    "UTC-02": "UTC-02:00",
    "UTC": "UTC",
    "UTC+12": "UTC+12:00",
    "UTC+13": "UTC+13:00",
}


def load_json(path):
    return json.loads(path.read_text(encoding="utf-8"))


def csharp_string(value):
    return (
        value.replace("\\", "\\\\")
        .replace('"', '\\"')
        .replace("\r", "\\r")
        .replace("\n", "\\n")
    )


def nested_get(data, parts):
    node = data
    for part in parts:
        if not isinstance(node, dict) or part not in node:
            return None
        node = node[part]
    return node


def windows_zones():
    data = load_json(CLDR / "supplemental-windowsZones.json")
    rows = data["supplemental"]["windowsZones"]["mapTimezones"]
    result = []
    for row in rows:
        zone = row["mapZone"]
        if zone["_territory"] == "001":
            result.append((zone["_other"], zone["_type"].split()[0]))
    return sorted(result, key=lambda item: item[0].lower())


def load_metazone_map():
    data = load_json(CLDR / "supplemental-metaZones.json")
    return data["supplemental"]["metaZones"]["metazoneInfo"]["timezone"]


def metazone_for(iana_id, metazone_map):
    entries = nested_get(metazone_map, iana_id.split("/"))
    if isinstance(entries, dict):
        entries = [entries]
    if not isinstance(entries, list):
        return None
    for entry in reversed(entries):
        info = entry.get("usesMetazone", {})
        if "_to" not in info:
            return info.get("_mzone")
    if entries:
        return entries[-1].get("usesMetazone", {}).get("_mzone")
    return None


def localized_metazone_name(time_zone_names, metazone_id):
    if not metazone_id:
        return None
    node = time_zone_names.get("metazone", {}).get(metazone_id)
    if not isinstance(node, dict):
        return None
    long_name = node.get("long")
    if not isinstance(long_name, dict):
        return None
    return long_name.get("generic") or long_name.get("standard") or long_name.get("daylight")


def localized_city(time_zone_names, iana_id, allow_ascii_fallback):
    node = nested_get(time_zone_names.get("zone", {}), iana_id.split("/"))
    if isinstance(node, dict) and isinstance(node.get("exemplarCity"), str):
        return node["exemplarCity"], True
    if allow_ascii_fallback:
        return iana_id.split("/")[-1].replace("_", " "), False
    return None, False


def build_name(windows_id, iana_id, time_zone_names, language_code, metazone_map):
    if windows_id in FIXED_ZONE_NAMES:
        return FIXED_ZONE_NAMES[windows_id]

    meta_name = localized_metazone_name(time_zone_names, metazone_for(iana_id, metazone_map))
    city, city_is_localized = localized_city(time_zone_names, iana_id, language_code == "en-US")

    if meta_name and city:
        if city.lower() not in meta_name.lower():
            if language_code == "en-US" or city_is_localized:
                return meta_name + " - " + city
        return meta_name
    if meta_name:
        return meta_name
    if city:
        return city
    return FIXED_ZONE_NAMES.get(windows_id, "UTC")


def generate_block():
    metazone_map = load_metazone_map()
    zones = windows_zones()
    language_tables = {}

    for language_code, (cldr_locale, file_name) in LOCALE_FILES.items():
        data = load_json(CLDR / file_name)
        time_zone_names = data["main"][cldr_locale]["dates"]["timeZoneNames"]
        table = {}
        for windows_id, iana_id in zones:
            table[windows_id] = build_name(windows_id, iana_id, time_zone_names, language_code, metazone_map)
        language_tables[language_code] = table

    lines = []
    lines.append("        private static readonly Dictionary<string, Dictionary<string, string>> LocalizedWindowsTimeZoneNames = new Dictionary<string, Dictionary<string, string>>")
    lines.append("        {")
    for language_code, table in language_tables.items():
        lines.append(f'            {{"{language_code}", new Dictionary<string, string>')
        lines.append("            {")
        for windows_id, name in table.items():
            lines.append(f'                {{"{csharp_string(windows_id)}", "{csharp_string(name)}"}},')
        lines.append("            }},")
    lines.append("        };")
    lines.append("")
    lines.append("        private static string LocalizedCommonName(string id, string languageCode)")
    lines.append("        {")
    lines.append("            Dictionary<string, string> names;")
    lines.append("            if (!LocalizedWindowsTimeZoneNames.TryGetValue(languageCode, out names))")
    lines.append("            {")
    lines.append('                names = LocalizedWindowsTimeZoneNames["en-US"];')
    lines.append("            }")
    lines.append("            string name;")
    lines.append("            return names.TryGetValue(id, out name) ? name : string.Empty;")
    lines.append("        }")
    return "\n".join(lines)


def replace_block():
    source = SRC.read_text(encoding="utf-8")
    start = source.index("        private static readonly Dictionary<string, Dictionary<string, string>> LocalizedWindowsTimeZoneNames")
    end = source.index("        private static string EnglishFallbackName", start)
    updated = source[:start] + generate_block() + "\n\n" + source[end:]
    SRC.write_text(updated, encoding="utf-8")


if __name__ == "__main__":
    replace_block()

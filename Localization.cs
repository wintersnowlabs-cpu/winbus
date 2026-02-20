namespace WinBus.Utility;

internal sealed record LanguageOption(string Code, string Name);

internal static class Localization
{
    public static string CurrentLanguageCode { get; private set; } = "en";

    public static IReadOnlyList<LanguageOption> SupportedLanguages { get; } =
    [
        new("en", "English"),
        new("cy", "Cymraeg (Welsh)"),
        new("bn", "বাংলা (Bengali)"),
        new("hi", "हिन्दी (Hindi)"),
        new("ta", "தமிழ் (Tamil)"),
        new("te", "తెలుగు (Telugu)"),
        new("es", "Español (Spanish)"),
        new("fr", "Français (French)"),
        new("de", "Deutsch (German)"),
        new("ar", "العربية (Arabic)")
    ];

    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = BuildEnglish(),
        ["cy"] = BuildWelsh(),
        ["bn"] = BuildBengali(),
        ["hi"] = BuildHindi(),
        ["ta"] = BuildTamil(),
        ["te"] = BuildTelugu(),
        ["es"] = BuildSpanish(),
        ["fr"] = BuildFrench(),
        ["de"] = BuildGerman(),
        ["ar"] = BuildArabic()
    };

    public static void SetLanguage(string code)
    {
        CurrentLanguageCode = Translations.ContainsKey(code) ? code : "en";
    }

    public static string T(string key)
    {
        if (Translations.TryGetValue(CurrentLanguageCode, out var selected) && selected.TryGetValue(key, out var value))
        {
            return value;
        }

        if (Translations["en"].TryGetValue(key, out var fallback))
        {
            return fallback;
        }

        return key;
    }

    private static Dictionary<string, string> BuildEnglish() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["header_title"] = "WinBus Utility - Windows 11 Pro Performance Tuning Suite",
        ["intro_line1"] = "This utility shows each module, explains impact, and asks your explicit consent before running.",
        ["intro_line2"] = "For best results, run as Administrator.",
        ["warning_not_admin"] = "Warning: Utility is not elevated. Some modules may fail without Administrator rights.",
        ["log_locations_header"] = "Log and monitoring paths:",
        ["log_startup_report"] = "- Startup audit report: {0}",
        ["log_watchdog_alerts"] = "- Watchdog alert log: {0}",
        ["log_remote_config"] = "- Remote monitoring config: {0}",
        ["log_remote_events"] = "- Remote monitoring event log: {0}",
        ["remote_template_created"] = "Created remote-monitoring.json template. Configure endpoint and set Enabled=true to activate remote status publishing.",
        ["remote_monitoring_enabled"] = "Remote monitoring: ENABLED | Endpoint: {0} | Heartbeat: {1}s",
        ["remote_monitoring_disabled"] = "Remote monitoring: DISABLED",
        ["language_select"] = "Select your language:",
        ["language_prompt"] = "Enter language number (default 1)",
        ["invalid_choice_default"] = "Invalid choice. Defaulting to English.",
        ["module_label"] = "Module",
        ["what_label"] = "What it does",
        ["benefit_label"] = "How it helps",
        ["needs_label"] = "Needs",
        ["consent_prompt"] = "Do you approve executing this module now?",
        ["skipped_by_user"] = "Skipped by user consent decision.",
        ["executing_module"] = "Executing module...",
        ["result_success"] = "Result: SUCCESS",
        ["result_failed"] = "Result: FAILED",
        ["execution_summary"] = "Execution summary:",
        ["status_skipped"] = "SKIPPED",
        ["status_success"] = "SUCCESS",
        ["status_failed"] = "FAILED",
        ["done_message"] = "Done. Review messages above for action details and system impact notes.",
        ["yes"] = "Yes",
        ["no"] = "No",
        ["m1_name"] = "Module 1 - Safety Checkpoint (Kernel Snapshot)",
        ["m1_desc"] = "Creates a Windows restore point using WMI SystemRestore.CreateRestorePoint.",
        ["m1_benefits"] = "Gives rollback safety before system-level changes.",
        ["m1_req"] = "Administrator rights and System Protection enabled.",
        ["m2_name"] = "Module 2 - Telemetry Engine Kill-Switch",
        ["m2_desc"] = "Stops DiagTrack service and sets AllowTelemetry policy DWORD to 0.",
        ["m2_benefits"] = "Can reduce background telemetry processing and idle CPU usage.",
        ["m2_req"] = "Administrator rights required.",
        ["m3_name"] = "Module 3 - Cache & Memory Purge",
        ["m3_desc"] = "Cleans user temp, system temp, and Windows SoftwareDistribution content.",
        ["m3_benefits"] = "Reclaims disk space and may reduce update/cache-related slowdowns.",
        ["m3_req"] = "Administrator rights recommended.",
        ["m4_name"] = "Module 4 - Startup Audit Agent",
        ["m4_desc"] = "Scans HKCU/HKLM Run keys and reports startup commands.",
        ["m4_benefits"] = "Identifies potential startup memory hogs, including Electron-based apps.",
        ["m4_req"] = "Read access to registry.",
        ["m5_name"] = "Module 5 - Tray Watchdog (Passive Monitor)",
        ["m5_desc"] = "Runs a 30-second interval tray monitor for high private memory use.",
        ["m5_benefits"] = "Provides early warning before heavy memory leaks degrade responsiveness.",
        ["m5_req"] = "Interactive desktop session required.",
        ["startup_none_summary"] = "No startup items found in the standard Run keys.",
        ["startup_checked_detail"] = "Checked HKCU and HKLM Run registry hives.",
        ["startup_found_summary"] = "Found {0} startup item(s). Electron-likely item(s): {1}.",
        ["startup_report_saved"] = "Detailed report saved: {0}",
        ["startup_review_tip"] = "Review high-memory startup apps and disable non-essential ones from Task Manager > Startup Apps.",
        ["report_command"] = "Command",
        ["report_electron_likely"] = "Electron likely",
        ["watchdog_intro1"] = "This module launches a low-frequency monitor every 30 seconds.",
        ["watchdog_intro2"] = "It will show a balloon tip if any process exceeds your memory threshold.",
        ["watchdog_threshold_prompt"] = "Enter memory threshold in MB (default 1200)",
        ["watchdog_starting"] = "Starting watchdog. Press Ctrl+C to stop and close tray icon.",
        ["watchdog_ended"] = "Tray watchdog session ended.",
        ["watchdog_threshold_used"] = "Threshold used: {0} MB",
        ["watchdog_interval_detail"] = "Watchdog checks running processes every 30 seconds.",
        ["watchdog_log_path"] = "Alert log path: {0}"
    };

    private static Dictionary<string, string> BuildWelsh()
    {
        var d = BuildEnglish();
        d["language_select"] = "Dewiswch eich iaith:";
        d["language_prompt"] = "Rhowch rif yr iaith (rhagosodedig 1)";
        d["consent_prompt"] = "A ydych yn cymeradwyo gweithredu'r modiwl hwn nawr?";
        d["skipped_by_user"] = "Wedi'i hepgor gan benderfyniad cydsyniad y defnyddiwr.";
        d["executing_module"] = "Yn gweithredu'r modiwl...";
        d["execution_summary"] = "Crynodeb gweithredu:";
        d["done_message"] = "Wedi gorffen. Adolygwch y negeseuon uchod am fanylion a nodiadau effaith.";
        return d;
    }

    private static Dictionary<string, string> BuildBengali()
    {
        var d = BuildEnglish();
        d["language_select"] = "আপনার ভাষা নির্বাচন করুন:";
        d["language_prompt"] = "ভাষার নম্বর লিখুন (ডিফল্ট 1)";
        d["consent_prompt"] = "আপনি কি এখন এই মডিউলটি চালাতে সম্মতি দিচ্ছেন?";
        d["skipped_by_user"] = "ব্যবহারকারীর সম্মতির সিদ্ধান্তে স্কিপ করা হয়েছে।";
        d["executing_module"] = "মডিউল চালানো হচ্ছে...";
        d["execution_summary"] = "কার্যসম্পাদনের সারাংশ:";
        d["done_message"] = "সম্পন্ন। উপরের বার্তাগুলো দেখে বিস্তারিত এবং প্রভাব নোট যাচাই করুন।";
        return d;
    }

    private static Dictionary<string, string> BuildHindi()
    {
        var d = BuildEnglish();
        d["language_select"] = "अपनी भाषा चुनें:";
        d["language_prompt"] = "भाषा नंबर दर्ज करें (डिफ़ॉल्ट 1)";
        d["consent_prompt"] = "क्या आप अभी इस मॉड्यूल को चलाने की अनुमति देते हैं?";
        d["skipped_by_user"] = "उपयोगकर्ता सहमति के अनुसार मॉड्यूल छोड़ा गया।";
        d["executing_module"] = "मॉड्यूल चल रहा है...";
        d["execution_summary"] = "निष्पादन सारांश:";
        d["done_message"] = "पूर्ण। कार्रवाई विवरण और सिस्टम प्रभाव के लिए ऊपर संदेश देखें।";
        return d;
    }

    private static Dictionary<string, string> BuildTamil()
    {
        var d = BuildEnglish();
        d["language_select"] = "உங்கள் மொழியைத் தேர்வு செய்யுங்கள்:";
        d["language_prompt"] = "மொழி எண்ணை உள்ளிடவும் (இயல்புநிலை 1)";
        d["consent_prompt"] = "இந்த தொகுதியை இப்போது இயக்க நீங்கள் ஒப்புக்கொள்கிறீர்களா?";
        d["skipped_by_user"] = "பயனர் ஒப்புதலின் அடிப்படையில் தவிர்க்கப்பட்டது.";
        d["executing_module"] = "தொகுதி இயங்குகிறது...";
        d["execution_summary"] = "இயக்கச் சுருக்கம்:";
        d["done_message"] = "முடிந்தது. நடவடிக்கை விவரங்கள் மற்றும் தாக்க குறிப்புகளுக்கு மேலே உள்ள செய்திகளை பார்க்கவும்.";
        return d;
    }

    private static Dictionary<string, string> BuildTelugu()
    {
        var d = BuildEnglish();
        d["language_select"] = "మీ భాషను ఎంచుకోండి:";
        d["language_prompt"] = "భాష సంఖ్యను నమోదు చేయండి (డిఫాల్ట్ 1)";
        d["consent_prompt"] = "ఈ మాడ్యూల్‌ను ఇప్పుడు అమలు చేయడానికి మీరు అంగీకరిస్తారా?";
        d["skipped_by_user"] = "వినియోగదారుడి సమ్మతి నిర్ణయంతో దాటవేయబడింది.";
        d["executing_module"] = "మాడ్యూల్ అమలవుతోంది...";
        d["execution_summary"] = "అమలు సారాంశం:";
        d["done_message"] = "పూర్తైంది. చర్య వివరాలు మరియు సిస్టమ్ ప్రభావం కోసం పై సందేశాలను చూడండి.";
        return d;
    }

    private static Dictionary<string, string> BuildSpanish()
    {
        var d = BuildEnglish();
        d["language_select"] = "Seleccione su idioma:";
        d["language_prompt"] = "Ingrese el número de idioma (predeterminado 1)";
        d["consent_prompt"] = "¿Aprueba ejecutar este módulo ahora?";
        d["skipped_by_user"] = "Omitido por decisión de consentimiento del usuario.";
        d["executing_module"] = "Ejecutando módulo...";
        d["execution_summary"] = "Resumen de ejecución:";
        d["done_message"] = "Finalizado. Revise los mensajes anteriores para detalles e impacto en el sistema.";
        return d;
    }

    private static Dictionary<string, string> BuildFrench()
    {
        var d = BuildEnglish();
        d["language_select"] = "Choisissez votre langue :";
        d["language_prompt"] = "Entrez le numéro de langue (par défaut 1)";
        d["consent_prompt"] = "Approuvez-vous l'exécution de ce module maintenant ?";
        d["skipped_by_user"] = "Ignoré selon la décision de consentement de l'utilisateur.";
        d["executing_module"] = "Exécution du module...";
        d["execution_summary"] = "Résumé d'exécution :";
        d["done_message"] = "Terminé. Consultez les messages ci-dessus pour les détails et l'impact système.";
        return d;
    }

    private static Dictionary<string, string> BuildGerman()
    {
        var d = BuildEnglish();
        d["language_select"] = "Sprache auswählen:";
        d["language_prompt"] = "Sprachnummer eingeben (Standard 1)";
        d["consent_prompt"] = "Möchten Sie dieses Modul jetzt ausführen?";
        d["skipped_by_user"] = "Aufgrund der Benutzerzustimmung übersprungen.";
        d["executing_module"] = "Modul wird ausgeführt...";
        d["execution_summary"] = "Ausführungsübersicht:";
        d["done_message"] = "Fertig. Prüfen Sie die obigen Meldungen für Details und Systemauswirkungen.";
        return d;
    }

    private static Dictionary<string, string> BuildArabic()
    {
        var d = BuildEnglish();
        d["language_select"] = "اختر لغتك:";
        d["language_prompt"] = "أدخل رقم اللغة (الافتراضي 1)";
        d["consent_prompt"] = "هل توافق على تنفيذ هذه الوحدة الآن؟";
        d["skipped_by_user"] = "تم التخطي بناءً على قرار موافقة المستخدم.";
        d["executing_module"] = "جارٍ تنفيذ الوحدة...";
        d["execution_summary"] = "ملخص التنفيذ:";
        d["done_message"] = "تم. راجع الرسائل أعلاه لتفاصيل الإجراءات وتأثير النظام.";
        return d;
    }
}

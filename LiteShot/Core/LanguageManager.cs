using System.Collections.Generic;

namespace LiteShot.Core
{
    /// <summary>
    /// Gerencia o sistema de internacionalização (i18n) do aplicativo.
    /// Facilita a tradução da interface para múltiplos idiomas via dicionário em memória.
    /// </summary>
    public static class LanguageManager
    {
        public static string CurrentLanguage = "pt-BR";

        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
        {
            ["pt-BR"] = new()
            {
                ["SettingsTitle"] = "Opções do LiteShot",
                ["ShowNotifications"] = "Mostrar notificações ao copiar/salvar",
                ["CaptureCursor"] = "Capturar cursor na imagem",
                ["ImgFormat"] = "Formato de Imagem:",
                ["HotkeyLabel"] = "Tecla de Atalho:",
                ["LangLabel"] = "Idioma:",
                ["BtnSave"] = "Salvar e Fechar",
                ["ToastCopied"] = "Copiado para a área de transferência!",
                ["ToastSaved"] = "Imagem guardada com sucesso!",
                ["DevMode"] = "Em desenvolvimento...",
                ["Caneta"] = "Caneta",
                ["Linha"] = "Linha",
                ["Seta"] = "Seta",
                ["Forma"] = "Forma",
                ["Marcador"] = "Marcador",
                ["Texto"] = "Texto",
                ["Cor"] = "Cor",
                ["Desfazer"] = "Desfazer",
                ["Refazer"] = "Refazer",
                ["Salvar"] = "Salvar",
                ["Copiar"] = "Copiar",
                ["Fechar"] = "Fechar",
                ["TelaCheia"] = "Expandir / Tela Cheia",
                ["Sobre"] = "Sobre o LiteShot",
                ["BtnReset"] = "Padrão",
                ["NavbarVertical"] = "Barra de Ferramentas Vertical",
                ["AppTooltip"] = "LiteShot",
                ["KeepSelection"] = "Manter posição da área selecionada",
                ["KeepNavbarPosition"] = "Manter posição da barra de ferramentas"
            },
            ["en-US"] = new()
            {
                ["SettingsTitle"] = "LiteShot Options",
                ["ShowNotifications"] = "Show notifications on copy/save",
                ["CaptureCursor"] = "Capture cursor in image",
                ["ImgFormat"] = "Image Format:",
                ["HotkeyLabel"] = "Hotkey:",
                ["LangLabel"] = "Language:",
                ["BtnSave"] = "Save & Close",
                ["ToastCopied"] = "Copied to clipboard!",
                ["ToastSaved"] = "Image saved successfully!",
                ["DevMode"] = "Under development...",
                ["Caneta"] = "Pen",
                ["Linha"] = "Line",
                ["Seta"] = "Arrow",
                ["Forma"] = "Shape",
                ["Marcador"] = "Highlighter",
                ["Texto"] = "Text",
                ["Cor"] = "Color",
                ["Desfazer"] = "Undo",
                ["Refazer"] = "Redo",
                ["Salvar"] = "Save",
                ["Copiar"] = "Copy",
                ["Fechar"] = "Close",
                ["TelaCheia"] = "Expand / Full Screen",
                ["Sobre"] = "About LiteShot",
                ["BtnReset"] = "Default",
                ["NavbarVertical"] = "Vertical Toolbar",
                ["AppTooltip"] = "LiteShot",
                ["KeepSelection"] = "Keep selected area position",
                ["KeepNavbarPosition"] = "Keep toolbar position"
            },
            ["es-ES"] = new()
            {
                ["SettingsTitle"] = "Opciones de LiteShot",
                ["ShowNotifications"] = "Mostrar notificaciones al copiar/guardar",
                ["CaptureCursor"] = "Capturar el cursor en la imagen",
                ["ImgFormat"] = "Formato de imagen:",
                ["HotkeyLabel"] = "Tecla de acceso rápido:",
                ["LangLabel"] = "Idioma:",
                ["BtnSave"] = "Guardar y cerrar",
                ["ToastCopied"] = "¡Copiado al portapapeles!",
                ["ToastSaved"] = "¡Imagen guardada con éxito!",
                ["DevMode"] = "En desarrollo...",
                ["Caneta"] = "Lápiz",
                ["Linha"] = "Línea",
                ["Seta"] = "Flecha",
                ["Forma"] = "Forma",
                ["Marcador"] = "Resaltador",
                ["Texto"] = "Texto",
                ["Cor"] = "Color",
                ["Desfazer"] = "Deshacer",
                ["Refazer"] = "Rehacer",
                ["Salvar"] = "Guardar",
                ["Copiar"] = "Copiar",
                ["Fechar"] = "Cerrar",
                ["TelaCheia"] = "Expandir / Pantalla Completa",
                ["Sobre"] = "Acerca de LiteShot",
                ["BtnReset"] = "Por defecto",
                ["NavbarVertical"] = "Barra de herramientas vertical",
                ["AppTooltip"] = "LiteShot",
                ["KeepSelection"] = "Mantener posición del área seleccionada",
                ["KeepNavbarPosition"] = "Mantener posición de la barra de herramientas"
            },
            ["fr-FR"] = new()
            {
                ["SettingsTitle"] = "Options LiteShot",
                ["ShowNotifications"] = "Afficher les notifications lors de la copie/sauvegarde",
                ["CaptureCursor"] = "Capturer le curseur dans l'image",
                ["ImgFormat"] = "Format d'image:",
                ["HotkeyLabel"] = "Raccourci:",
                ["LangLabel"] = "Langue:",
                ["BtnSave"] = "Enregistrer et fermer",
                ["ToastCopied"] = "Copié dans le presse-papiers!",
                ["ToastSaved"] = "Image enregistrée avec succès!",
                ["DevMode"] = "En cours de développement...",
                ["Caneta"] = "Stylo",
                ["Linha"] = "Ligne",
                ["Seta"] = "Flèche",
                ["Forma"] = "Forme",
                ["Marcador"] = "Surligneur",
                ["Texto"] = "Texte",
                ["Cor"] = "Couleur",
                ["Desfazer"] = "Annuler",
                ["Refazer"] = "Rétablir",
                ["Salvar"] = "Enregistrer",
                ["Copiar"] = "Copier",
                ["Fechar"] = "Fermer",
                ["TelaCheia"] = "Agrandir / Plein Écran",
                ["Sobre"] = "À propos de LiteShot",
                ["BtnReset"] = "Défaut",
                ["NavbarVertical"] = "Barre d'outils verticale",
                ["AppTooltip"] = "LiteShot",
                ["KeepSelection"] = "Conserver la position de la zone sélectionnée",
                ["KeepNavbarPosition"] = "Conserver la position de la barre d'outils"
            },
            ["de-DE"] = new()
            {
                ["SettingsTitle"] = "LiteShot-Optionen",
                ["ShowNotifications"] = "Benachrichtigungen beim Kopieren/Speichern anzeigen",
                ["CaptureCursor"] = "Cursor im Bild erfassen",
                ["ImgFormat"] = "Bildformat:",
                ["HotkeyLabel"] = "Hotkey:",
                ["LangLabel"] = "Sprache:",
                ["BtnSave"] = "Speichern & Schließen",
                ["ToastCopied"] = "In die Zwischenablage kopiert!",
                ["ToastSaved"] = "Bild erfolgreich gespeichert!",
                ["DevMode"] = "In Entwicklung...",
                ["Caneta"] = "Stift",
                ["Linha"] = "Linie",
                ["Seta"] = "Pfeil",
                ["Forma"] = "Form",
                ["Marcador"] = "Textmarker",
                ["Texto"] = "Text",
                ["Cor"] = "Farbe",
                ["Desfazer"] = "Rückgängig",
                ["Refazer"] = "Wiederholen",
                ["Salvar"] = "Speichern",
                ["Copiar"] = "Kopieren",
                ["Fechar"] = "Schließen",
                ["TelaCheia"] = "Erweitern / Vollbild",
                ["Sobre"] = "Über LiteShot",
                ["BtnReset"] = "Standard",
                ["NavbarVertical"] = "Vertikale Symbolleiste",
                ["AppTooltip"] = "LiteShot",
                ["KeepSelection"] = "Position des ausgewählten Bereichs beibehalten",
                ["KeepNavbarPosition"] = "Position der Symbolleiste beibehalten"
            },
            ["it-IT"] = new()
            {
                ["SettingsTitle"] = "Opzioni LiteShot",
                ["ShowNotifications"] = "Mostra notifiche durante copia/salvataggio",
                ["CaptureCursor"] = "Cattura cursore nell'immagine",
                ["ImgFormat"] = "Formato immagine:",
                ["HotkeyLabel"] = "Scelta rapida:",
                ["LangLabel"] = "Lingua:",
                ["BtnSave"] = "Salva e Chiudi",
                ["ToastCopied"] = "Copiato negli appunti!",
                ["ToastSaved"] = "Immagine salvata con successo!",
                ["DevMode"] = "In fase di sviluppo...",
                ["Caneta"] = "Penna",
                ["Linha"] = "Linea",
                ["Seta"] = "Freccia",
                ["Forma"] = "Forma",
                ["Marcador"] = "Evidenziatore",
                ["Texto"] = "Testo",
                ["Cor"] = "Colore",
                ["Desfazer"] = "Annulla",
                ["Refazer"] = "Ripeti",
                ["Salvar"] = "Salva",
                ["Copiar"] = "Copia",
                ["Fechar"] = "Chiudi",
                ["TelaCheia"] = "Espandi / Schermo Intero",
                ["Sobre"] = "Info su LiteShot",
                ["BtnReset"] = "Predefinito",
                ["NavbarVertical"] = "Barra degli strumenti verticale",
                ["AppTooltip"] = "LiteShot",
                ["KeepSelection"] = "Mantieni posizione dell'area selezionata",
                ["KeepNavbarPosition"] = "Mantieni posizione della barra degli strumenti"
            }
        };

        /// <summary>
        /// Procura a tradução baseada na chave e no idioma atual.
        /// </summary>
        public static string GetString(string key)
        {
            if (Translations.ContainsKey(CurrentLanguage) && Translations[CurrentLanguage].ContainsKey(key))
                return Translations[CurrentLanguage][key];
            return key;
        }
    }
}
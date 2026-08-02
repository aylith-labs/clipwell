use std::time::Duration;

use enigo::{Direction, Enigo, Key, Keyboard, Settings as EnigoSettings};
use tauri::menu::{Menu, MenuItem};
use tauri::tray::{MouseButton, TrayIconBuilder, TrayIconEvent};
use tauri::{AppHandle, Manager, WebviewWindow, WindowEvent};
use tauri_plugin_global_shortcut::{Code, GlobalShortcutExt, Modifiers, Shortcut, ShortcutState};

fn show_picker(app: &AppHandle) {
    if let Some(window) = app.get_webview_window("main") {
        let _ = window.show();
        let _ = window.set_focus();
    }
}

#[tauri::command]
fn hide_window(window: WebviewWindow) {
    let _ = window.hide();
}

#[tauri::command]
fn open_external(target: String) {
    let _ = open::that(target);
}

/// Hide the picker, let focus return to the previous app, then synthesize paste.
#[tauri::command]
fn paste_and_hide(app: AppHandle) {
    if let Some(window) = app.get_webview_window("main") {
        let _ = window.hide();
    }
    std::thread::sleep(Duration::from_millis(120));
    if let Ok(mut enigo) = Enigo::new(&EnigoSettings::default()) {
        #[cfg(target_os = "macos")]
        let modifier = Key::Meta;
        #[cfg(not(target_os = "macos"))]
        let modifier = Key::Control;
        let _ = enigo.key(modifier, Direction::Press);
        let _ = enigo.key(Key::Unicode('v'), Direction::Click);
        let _ = enigo.key(modifier, Direction::Release);
    }
}

/// Re-register the global hotkey from a chord string like "Alt+Shift+V".
///
/// The chord is parsed before anything is unregistered: an unrecognized chord
/// leaves the working hotkey in place and reports the error, rather than
/// silently leaving the user with no way to summon the picker.
#[tauri::command]
fn set_hotkey(app: AppHandle, chord: String) -> Result<(), String> {
    let shortcut = parse_chord(&chord).ok_or_else(|| format!("unrecognized hotkey chord: {chord}"))?;
    let shortcuts = app.global_shortcut();
    let _ = shortcuts.unregister_all();
    shortcuts.register(shortcut).map_err(|error| error.to_string())
}

/// Parses a chord like "Alt+Shift+V". Every segment must be a known modifier or
/// key — an unrecognized one rejects the whole chord instead of being dropped.
fn parse_chord(chord: &str) -> Option<Shortcut> {
    let mut modifiers = Modifiers::empty();
    let mut code: Option<Code> = None;
    for part in chord.split('+') {
        match part.trim().to_ascii_lowercase().as_str() {
            "alt" | "option" | "opt" => modifiers |= Modifiers::ALT,
            "ctrl" | "control" => modifiers |= Modifiers::CONTROL,
            "shift" => modifiers |= Modifiers::SHIFT,
            "win" | "super" | "cmd" | "meta" => modifiers |= Modifiers::SUPER,
            key => match key_to_code(key) {
                // A second key would mean an ambiguous chord like "V+B".
                Some(parsed) if code.is_none() => code = Some(parsed),
                _ => return None,
            },
        }
    }
    code.map(|key| Shortcut::new(Some(modifiers), key))
}

fn key_to_code(key: &str) -> Option<Code> {
    Some(match key.to_ascii_uppercase().as_str() {
        "A" => Code::KeyA, "B" => Code::KeyB, "C" => Code::KeyC, "D" => Code::KeyD,
        "E" => Code::KeyE, "F" => Code::KeyF, "G" => Code::KeyG, "H" => Code::KeyH,
        "I" => Code::KeyI, "J" => Code::KeyJ, "K" => Code::KeyK, "L" => Code::KeyL,
        "M" => Code::KeyM, "N" => Code::KeyN, "O" => Code::KeyO, "P" => Code::KeyP,
        "Q" => Code::KeyQ, "R" => Code::KeyR, "S" => Code::KeyS, "T" => Code::KeyT,
        "U" => Code::KeyU, "V" => Code::KeyV, "W" => Code::KeyW, "X" => Code::KeyX,
        "Y" => Code::KeyY, "Z" => Code::KeyZ,
        "0" => Code::Digit0, "1" => Code::Digit1, "2" => Code::Digit2, "3" => Code::Digit3,
        "4" => Code::Digit4, "5" => Code::Digit5, "6" => Code::Digit6, "7" => Code::Digit7,
        "8" => Code::Digit8, "9" => Code::Digit9,
        "F1" => Code::F1, "F2" => Code::F2, "F3" => Code::F3, "F4" => Code::F4,
        "F5" => Code::F5, "F6" => Code::F6, "F7" => Code::F7, "F8" => Code::F8,
        "F9" => Code::F9, "F10" => Code::F10, "F11" => Code::F11, "F12" => Code::F12,
        _ => return None,
    })
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_single_instance::init(|app, _argv, _cwd| {
            show_picker(app);
        }))
        .plugin(
            tauri_plugin_global_shortcut::Builder::new()
                .with_handler(|app, _shortcut, event| {
                    if event.state() == ShortcutState::Pressed {
                        // Capture-time focus return happens via the OS; just show.
                        show_picker(app);
                    }
                })
                .build(),
        )
        .invoke_handler(tauri::generate_handler![
            hide_window,
            open_external,
            paste_and_hide,
            set_hotkey
        ])
        .setup(|app| {
            // Tray icon with Show / Quit.
            let show_i = MenuItem::with_id(app, "show", "Show picker", true, None::<&str>)?;
            let quit_i = MenuItem::with_id(app, "quit", "Quit Clipwell", true, None::<&str>)?;
            let menu = Menu::with_items(app, &[&show_i, &quit_i])?;
            TrayIconBuilder::new()
                .icon(app.default_window_icon().unwrap().clone())
                .tooltip("Clipwell")
                .menu(&menu)
                .on_menu_event(|app, event| match event.id.as_ref() {
                    "show" => show_picker(app),
                    "quit" => app.exit(0),
                    _ => {}
                })
                .on_tray_icon_event(|tray, event| {
                    if let TrayIconEvent::Click { button: MouseButton::Left, .. } = event {
                        show_picker(tray.app_handle());
                    }
                })
                .build(app)?;

            // Default global hotkey (the frontend re-registers from settings via set_hotkey).
            if let Some(shortcut) = parse_chord("Alt+Shift+V") {
                let _ = app.global_shortcut().register(shortcut);
            }

            // Start hidden (the window is visible:false in tauri.conf). The picker is
            // summoned by the global hotkey or the tray — never popped up on launch,
            // so it can't sit on top of the user's work uninvited.
            Ok(())
        })
        .on_window_event(|window, event| {
            // Hide on blur (picker behavior). Disable with CLIPWELL_NO_AUTOHIDE.
            if let WindowEvent::Focused(false) = event
                && std::env::var("CLIPWELL_NO_AUTOHIDE").is_err()
            {
                let _ = window.hide();
            }
        })
        .run(tauri::generate_context!())
        .expect("error while running Clipwell");
}

#[cfg(test)]
mod tests {
    use super::*;

    fn parsed(chord: &str) -> Shortcut {
        parse_chord(chord).expect("chord should parse")
    }

    #[test]
    fn parses_the_default_chord() {
        let shortcut = parsed("Alt+Shift+V");
        assert_eq!(shortcut.mods, Modifiers::ALT | Modifiers::SHIFT);
        assert_eq!(shortcut.key, Code::KeyV);
    }

    #[test]
    fn accepts_every_modifier_spelling() {
        assert_eq!(parsed("option+v").mods, Modifiers::ALT);
        assert_eq!(parsed("control+v").mods, Modifiers::CONTROL);
        assert_eq!(parsed("cmd+v").mods, Modifiers::SUPER);
        assert_eq!(parsed("super+v").mods, Modifiers::SUPER);
    }

    #[test]
    fn is_case_and_whitespace_insensitive() {
        assert_eq!(parsed("  ALT + shift + v  ").key, Code::KeyV);
    }

    #[test]
    fn accepts_digit_and_function_keys() {
        assert_eq!(parsed("Ctrl+1").key, Code::Digit1);
        assert_eq!(parsed("Ctrl+F12").key, Code::F12);
    }

    #[test]
    fn accepts_a_bare_key_with_no_modifiers() {
        assert_eq!(parsed("F9").mods, Modifiers::empty());
    }

    #[test]
    fn rejects_an_unknown_key_instead_of_dropping_it() {
        // Dropping it would silently register a different hotkey than the one
        // the user asked for.
        assert!(parse_chord("Alt+Shift+Delete").is_none());
    }

    #[test]
    fn rejects_a_trailing_separator() {
        assert!(parse_chord("Alt+Shift+V+").is_none());
    }

    #[test]
    fn rejects_a_chord_with_no_key() {
        assert!(parse_chord("Alt+Shift").is_none());
        assert!(parse_chord("").is_none());
    }

    #[test]
    fn rejects_two_keys() {
        assert!(parse_chord("Alt+V+B").is_none());
    }

    #[test]
    fn unknown_key_does_not_clobber_an_earlier_valid_key() {
        // The regression: the last segment used to overwrite `code` outright, so
        // a trailing unknown segment turned a valid chord into no chord at all —
        // and set_hotkey had already unregistered the working one by then.
        assert!(parse_chord("Alt+V+Nonsense").is_none());
        assert_eq!(parsed("Alt+V").key, Code::KeyV);
    }
}

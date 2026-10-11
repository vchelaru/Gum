import Cocoa
import ApplicationServices

// gumdrv: input injection and accessibility queries for the macOS pass (Tools/MacPass/README.md).
// Needs the Accessibility permission for the app that launches it. Coordinates are screen points.
//
//   click X Y [right|double]   move X Y   drag X1 Y1 X2 Y2   scroll X Y LINES
//   key NAME [cmd,shift,alt,ctrl]   type TEXT
//   activate PID   active PID   title PID   windows PID   tree PID [maxDepth]
//   setframe PID X Y W H             move and resize the first window
//   menu PID Top [Sub ...] Leaf      press a menu item by its title path (no coordinates)
//   menuinfo PID                     every menu item: path|enabled|cmdChar|cmdModifiers

func post(_ e: CGEvent?) { e?.post(tap: .cghidEventTap); usleep(15000) }

func mouse(_ type: CGEventType, _ x: Double, _ y: Double, _ btn: CGMouseButton = .left, clicks: Int64 = 1) {
    let e = CGEvent(mouseEventSource: nil, mouseType: type, mouseCursorPosition: CGPoint(x: x, y: y), mouseButton: btn)
    e?.setIntegerValueField(.mouseEventClickState, value: clicks)
    post(e)
}

let keyCodes: [String: CGKeyCode] = [
    "a":0,"s":1,"d":2,"f":3,"h":4,"g":5,"z":6,"x":7,"c":8,"v":9,"b":11,"q":12,"w":13,"e":14,"r":15,"y":16,"t":17,
    "1":18,"2":19,"3":20,"4":21,"6":22,"5":23,"=":24,"9":25,"7":26,"-":27,"8":28,"0":29,"]":30,"o":31,"u":32,"[":33,
    "i":34,"p":35,"return":36,"l":37,"j":38,"'":39,"k":40,";":41,"\\":42,",":43,"/":44,"n":45,"m":46,".":47,
    "tab":48,"space":49,"`":50,"delete":51,"escape":53,"f1":122,"f2":120,"f3":99,"f4":118,"f5":96,"f6":97,"f7":98,"f8":100,
    "f9":101,"f10":109,"f11":103,"f12":111,"left":123,"right":124,"down":125,"up":126,"forwarddelete":117,"home":115,"end":119,
]

let shiftedKeys: [Character: Character] = [
    "!":"1","@":"2","#":"3","$":"4","%":"5","^":"6","&":"7","*":"8","(":"9",")":"0","_":"-","+":"=",
    "{":"[","}":"]",":":";","\"":"'","<":",",">":".","?":"/","|":"\\","~":"`",
]

func flags(_ s: String) -> CGEventFlags {
    var f: CGEventFlags = []
    for p in s.split(separator: ",") {
        switch p {
        case "cmd": f.insert(.maskCommand)
        case "shift": f.insert(.maskShift)
        case "alt": f.insert(.maskAlternate)
        case "ctrl": f.insert(.maskControl)
        default: break
        }
    }
    return f
}

func keyPress(_ code: CGKeyCode, _ f: CGEventFlags) {
    let d = CGEvent(keyboardEventSource: nil, virtualKey: code, keyDown: true); d?.flags = f; post(d)
    let u = CGEvent(keyboardEventSource: nil, virtualKey: code, keyDown: false); u?.flags = f; post(u)
}

func attr(_ el: AXUIElement, _ name: String) -> AnyObject? {
    var v: CFTypeRef?
    return AXUIElementCopyAttributeValue(el, name as CFString, &v) == .success ? (v as AnyObject?) : nil
}

func kids(_ el: AXUIElement) -> [AXUIElement] { (attr(el, kAXChildrenAttribute) as? [AXUIElement]) ?? [] }
func role(_ el: AXUIElement) -> String { (attr(el, kAXRoleAttribute) as? String) ?? "" }
func title(_ el: AXUIElement) -> String { (attr(el, kAXTitleAttribute) as? String) ?? "" }

func frame(_ el: AXUIElement) -> String {
    var p = CGPoint.zero, s = CGSize.zero
    if let pv = attr(el, kAXPositionAttribute), CFGetTypeID(pv) == AXValueGetTypeID() { AXValueGetValue(pv as! AXValue, .cgPoint, &p) }
    if let sv = attr(el, kAXSizeAttribute), CFGetTypeID(sv) == AXValueGetTypeID() { AXValueGetValue(sv as! AXValue, .cgSize, &s) }
    return "\(Int(p.x)) \(Int(p.y)) \(Int(s.width)) \(Int(s.height))"
}

func dump(_ el: AXUIElement, _ depth: Int, _ maxDepth: Int) {
    let desc = (attr(el, kAXDescriptionAttribute) as? String) ?? ""
    print(String(repeating: "  ", count: depth) + role(el)
        + (title(el).isEmpty ? "" : " title=\"\(title(el))\"")
        + (desc.isEmpty ? "" : " desc=\"\(desc)\"") + " [" + frame(el) + "]")
    if depth < maxDepth { for k in kids(el) { dump(k, depth + 1, maxDepth) } }
}

// The items of the menu hanging off a menu bar item or menu item.
func menuItems(of el: AXUIElement) -> [AXUIElement] {
    kids(el).filter { role($0) == "AXMenu" }.flatMap { kids($0) }
}

func menuBarItems(_ app: AXUIElement) -> [AXUIElement] {
    guard let bar = attr(app, kAXMenuBarAttribute) else { return [] }
    return kids(bar as! AXUIElement)
}

func dumpMenu(_ el: AXUIElement, _ path: String) {
    for item in menuItems(of: el) {
        let t = title(item)
        if t.isEmpty { continue }
        let enabled = (attr(item, kAXEnabledAttribute) as? Bool) ?? false
        let cmd = (attr(item, "AXMenuItemCmdChar") as? String) ?? ""
        let mods = (attr(item, "AXMenuItemCmdModifiers") as? NSNumber)?.intValue ?? -1
        print("\(path)\(t)|\(enabled)|\(cmd)|\(cmd.isEmpty ? "" : String(mods))")
        dumpMenu(item, path + t + ">")
    }
}

let a = CommandLine.arguments
guard a.count >= 2 else { print("usage: see header"); exit(1) }
switch a[1] {
case "click":
    let x = Double(a[2])!, y = Double(a[3])!
    let mode = a.count > 4 ? a[4] : "left"
    mouse(.mouseMoved, x, y)
    if mode == "right" { mouse(.rightMouseDown, x, y, .right); mouse(.rightMouseUp, x, y, .right) }
    else if mode == "double" {
        mouse(.leftMouseDown, x, y); mouse(.leftMouseUp, x, y)
        mouse(.leftMouseDown, x, y, clicks: 2); mouse(.leftMouseUp, x, y, clicks: 2)
    } else { mouse(.leftMouseDown, x, y); mouse(.leftMouseUp, x, y) }
case "move":
    mouse(.mouseMoved, Double(a[2])!, Double(a[3])!)
case "drag":
    // Slow, with a small initial wiggle: macOS runs a drag session in a nested event loop that
    // needs real-looking motion before it takes over.
    let x1 = Double(a[2])!, y1 = Double(a[3])!, x2 = Double(a[4])!, y2 = Double(a[5])!
    mouse(.mouseMoved, x1, y1); usleep(200000)
    mouse(.leftMouseDown, x1, y1); usleep(250000)
    for d in [2.0, 5.0, 9.0, 14.0] { mouse(.leftMouseDragged, x1 + d, y1 + d / 2); usleep(60000) }
    for i in 1...40 {
        let t = Double(i) / 40
        mouse(.leftMouseDragged, x1 + 14 + (x2 - x1 - 14) * t, y1 + 7 + (y2 - y1 - 7) * t); usleep(30000)
    }
    usleep(300000)
    mouse(.leftMouseUp, x2, y2)
case "key":
    guard let code = keyCodes[a[2].lowercased()] else { print("unknown key \(a[2])"); exit(2) }
    keyPress(code, a.count > 3 ? flags(a[3]) : [])
case "type":
    for ch in a[2] {
        var base = ch, shift = false
        if ch.isUppercase { base = Character(ch.lowercased()); shift = true }
        else if let b = shiftedKeys[ch] { base = b; shift = true }
        if let code = keyCodes[String(base)] { keyPress(code, shift ? .maskShift : []) }
        else {
            // No key code for this character; send it as a unicode string.
            let u = Array(String(ch).utf16)
            let d = CGEvent(keyboardEventSource: nil, virtualKey: 0, keyDown: true); d?.keyboardSetUnicodeString(stringLength: u.count, unicodeString: u); post(d)
            let up = CGEvent(keyboardEventSource: nil, virtualKey: 0, keyDown: false); up?.keyboardSetUnicodeString(stringLength: u.count, unicodeString: u); post(up)
        }
        usleep(8000)
    }
case "scroll":
    mouse(.mouseMoved, Double(a[2])!, Double(a[3])!)
    post(CGEvent(scrollWheelEvent2Source: nil, units: .line, wheelCount: 1, wheel1: Int32(a[4])!, wheel2: 0, wheel3: 0))
case "activate":
    NSRunningApplication(processIdentifier: pid_t(a[2])!)?.activate(options: [.activateIgnoringOtherApps])
case "active":
    print(NSRunningApplication(processIdentifier: pid_t(a[2])!)?.isActive ?? false)
case "title":
    let app = AXUIElementCreateApplication(pid_t(a[2])!)
    if let ws = attr(app, kAXWindowsAttribute) as? [AXUIElement], let w = ws.first { print(title(w)) }
case "windows":
    // One line per window: title|x y w h|subrole
    let app = AXUIElementCreateApplication(pid_t(a[2])!)
    for w in (attr(app, kAXWindowsAttribute) as? [AXUIElement]) ?? [] {
        print("\(title(w))|\(frame(w))|\((attr(w, kAXSubroleAttribute) as? String) ?? "")")
    }
case "setframe":
    let app = AXUIElementCreateApplication(pid_t(a[2])!)
    guard let w = (attr(app, kAXWindowsAttribute) as? [AXUIElement])?.first else { print("no window"); exit(3) }
    var p = CGPoint(x: Double(a[3])!, y: Double(a[4])!), sz = CGSize(width: Double(a[5])!, height: Double(a[6])!)
    AXUIElementSetAttributeValue(w, kAXPositionAttribute as CFString, AXValueCreate(.cgPoint, &p)!)
    AXUIElementSetAttributeValue(w, kAXSizeAttribute as CFString, AXValueCreate(.cgSize, &sz)!)
case "tree":
    dump(AXUIElementCreateApplication(pid_t(a[2])!), 0, a.count > 3 ? Int(a[3])! : 12)
case "menu":
    let app = AXUIElementCreateApplication(pid_t(a[2])!)
    let path = Array(a[3...])
    guard let top = menuBarItems(app).first(where: { title($0) == path[0] }) else { print("no menu \(path[0])"); exit(3) }
    var cur = top
    for name in path.dropFirst() {
        guard let next = menuItems(of: cur).first(where: { title($0) == name }) else { print("no item \(name)"); exit(3) }
        cur = next
    }
    let r = AXUIElementPerformAction(cur, kAXPressAction as CFString)
    print(r == .success ? "pressed" : "press failed \(r.rawValue)")
    exit(r == .success ? 0 : 4)
case "menuinfo":
    let app = AXUIElementCreateApplication(pid_t(a[2])!)
    for top in menuBarItems(app) where !title(top).isEmpty && title(top) != "Apple" {
        print("\(title(top))|true||")
        dumpMenu(top, title(top) + ">")
    }
default:
    print("unknown command \(a[1])"); exit(1)
}

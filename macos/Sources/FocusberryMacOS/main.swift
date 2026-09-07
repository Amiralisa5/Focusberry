import SwiftUI
import WebKit

@main
struct FocusberryMacOSApp: App {
    var body: some Scene {
        WindowGroup("Focusberry") {
            FocusberryWebView()
                .frame(minWidth: 1050, minHeight: 720)
        }
    }
}

struct FocusberryWebView: NSViewRepresentable {
    func makeCoordinator() -> Coordinator { Coordinator() }

    func makeNSView(context: Context) -> WKWebView {
        let configuration = WKWebViewConfiguration()
        configuration.websiteDataStore = .default()
        configuration.preferences.javaScriptCanOpenWindowsAutomatically = true

        let webView = WKWebView(frame: .zero, configuration: configuration)
        webView.navigationDelegate = context.coordinator
        webView.uiDelegate = context.coordinator

        guard let url = Bundle.main.url(forResource: "Focusberry", withExtension: "html") else {
            webView.loadHTMLString("<h1>Focusberry.html was not bundled.</h1>", baseURL: nil)
            return webView
        }

        webView.loadFileURL(url, allowingReadAccessTo: url.deletingLastPathComponent())
        return webView
    }

    func updateNSView(_ webView: WKWebView, context: Context) {}

    final class Coordinator: NSObject, WKNavigationDelegate, WKUIDelegate {
        func webView(_ webView: WKWebView,
                     decidePolicyFor navigationAction: WKNavigationAction,
                     decisionHandler: @escaping (WKNavigationActionPolicy) -> Void) {
            guard let url = navigationAction.request.url else {
                decisionHandler(.cancel)
                return
            }

            if url.isFileURL || url.scheme == "about" {
                decisionHandler(.allow)
            } else {
                NSWorkspace.shared.open(url)
                decisionHandler(.cancel)
            }
        }

        func webView(_ webView: WKWebView,
                     createWebViewWith configuration: WKWebViewConfiguration,
                     for navigationAction: WKNavigationAction,
                     windowFeatures: WKWindowFeatures) -> WKWebView? {
            if let url = navigationAction.request.url {
                NSWorkspace.shared.open(url)
            }
            return nil
        }
    }
}

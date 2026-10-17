import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'screens/library_screen.dart';
import 'services/reading_api.dart';
import 'services/session_store.dart';
import 'widgets/mochi_theme.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  final preferences = await SharedPreferences.getInstance();
  runApp(ToeicReadingApp(session: SessionStore(preferences)));
}

class ToeicReadingApp extends StatelessWidget {
  const ToeicReadingApp({super.key, required this.session, this.api});
  final SessionStore session;
  final ReadingApi? api;
  @override
  Widget build(BuildContext context) => MaterialApp(
    title: 'Mochi Reading',
    debugShowCheckedModeBanner: false,
    theme: Mochi.theme,
    home: _SessionRoot(session: session, api: api),
  );
}

class _SessionRoot extends StatefulWidget {
  const _SessionRoot({required this.session, this.api});
  final SessionStore session;
  final ReadingApi? api;
  @override
  State<_SessionRoot> createState() => _SessionRootState();
}

class _SessionRootState extends State<_SessionRoot> {
  late final ReadingApi api = widget.api ?? ReadingApi();
  bool ready = false;
  Object? error;
  @override
  void initState() {
    super.initState();
    api.onSessionExpired = () => widget.session.openSession(api);
    openSession();
  }

  Future<void> openSession() async {
    setState(() {
      ready = false;
      error = null;
    });
    try {
      await widget.session.openSession(api);
      if (mounted) setState(() => ready = true);
    } catch (e) {
      if (mounted) setState(() => error = e);
    }
  }

  @override
  void dispose() {
    api.close();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => ready
      ? LibraryScreen(api: api, session: widget.session)
      : Scaffold(
          body: error != null
              ? ReadingError(error!, retry: openSession)
              : const Center(child: CircularProgressIndicator()),
        );
}

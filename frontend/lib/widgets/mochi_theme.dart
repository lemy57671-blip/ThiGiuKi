import 'package:flutter/material.dart';

class Mochi {
  static const primary = Color(0xff584bbc),
      accent = Color(0xff8175e8),
      canvas = Color(0xfff9f9ff),
      lavender = Color(0xfff1eeff),
      mint = Color(0xffbae9db),
      ink = Color(0xff0f1c30),
      muted = Color(0xff728096),
      border = Color(0xffe8eaf2),
      correct = Color(0xff249a74),
      wrong = Color(0xffd95365),
      warning = Color(0xffe49a30);
  static ThemeData get theme => ThemeData(
    useMaterial3: true,
    fontFamily: 'BeVietnamPro',
    scaffoldBackgroundColor: canvas,
    colorScheme: ColorScheme.fromSeed(
      seedColor: primary,
      primary: primary,
      surface: Colors.white,
    ),
    appBarTheme: const AppBarTheme(
      backgroundColor: canvas,
      foregroundColor: ink,
      centerTitle: false,
      titleTextStyle: TextStyle(
        fontFamily: 'BeVietnamPro',
        fontSize: 17,
        fontWeight: FontWeight.w600,
        color: ink,
      ),
    ),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        minimumSize: const Size(0, 48),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        minimumSize: const Size(0, 48),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        side: const BorderSide(color: border),
      ),
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: Colors.white,
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: border),
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: border),
      ),
    ),
    dividerColor: border,
  );
}

class MochiCard extends StatelessWidget {
  const MochiCard({
    super.key,
    required this.child,
    this.color = Colors.white,
    this.padding = const EdgeInsets.all(20),
  });
  final Widget child;
  final Color color;
  final EdgeInsets padding;
  @override
  Widget build(BuildContext context) => Container(
    padding: padding,
    decoration: BoxDecoration(
      color: color,
      borderRadius: BorderRadius.circular(18),
      border: Border.all(color: Mochi.border.withValues(alpha: .5)),
    ),
    child: child,
  );
}

class Pill extends StatelessWidget {
  const Pill(
    this.text, {
    super.key,
    this.color = Mochi.lavender,
    this.foreground = Mochi.primary,
    this.icon,
  });
  final String text;
  final Color color, foreground;
  final IconData? icon;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
    decoration: BoxDecoration(
      color: color,
      borderRadius: BorderRadius.circular(99),
    ),
    child: Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        if (icon != null) ...[
          Icon(icon, size: 14, color: foreground),
          const SizedBox(width: 4),
        ],
        Flexible(
          child: Text(
            text,
            style: TextStyle(
              fontSize: 11,
              color: foreground,
              fontWeight: FontWeight.w600,
            ),
          ),
        ),
      ],
    ),
  );
}

class ReadingError extends StatelessWidget {
  const ReadingError(this.error, {super.key, required this.retry});
  final Object error;
  final VoidCallback retry;
  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(24),
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 520),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.cloud_off_rounded, size: 48, color: Mochi.muted),
            const SizedBox(height: 16),
            Text('$error', textAlign: TextAlign.center),
            const SizedBox(height: 20),
            FilledButton.icon(
              onPressed: retry,
              icon: const Icon(Icons.refresh),
              label: const Text('Thử lại'),
            ),
          ],
        ),
      ),
    ),
  );
}

class MobilePage extends StatelessWidget {
  const MobilePage({super.key, required this.child, this.maxWidth = 680});
  final Widget child;
  final double maxWidth;
  @override
  Widget build(BuildContext context) => Align(
    alignment: Alignment.topCenter,
    child: ConstrainedBox(
      constraints: BoxConstraints(maxWidth: maxWidth),
      child: child,
    ),
  );
}

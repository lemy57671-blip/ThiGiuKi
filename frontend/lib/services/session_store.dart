import 'dart:math';
import 'package:shared_preferences/shared_preferences.dart';
import 'reading_api.dart';

class SessionStore {
  SessionStore(this.preferences);
  final SharedPreferences preferences;
  String? get token => preferences.getString('reading.jwt');
  int get tenantId => preferences.getInt('reading.tenant') ?? 1;
  String get username => preferences.getString('reading.username') ?? '';
  String? get activeAttempt => preferences.getString('reading.activeAttempt');
  String? get visitorKey => preferences.getString('reading.visitorKey');
  bool get hasSession =>
      visitorKey != null &&
      token != null &&
      (preferences.getInt('reading.tokenExpires') ?? 0) >
          DateTime.now().millisecondsSinceEpoch;
  Future<void> openSession(ReadingApi api) async {
    final previousKey = visitorKey;
    final result = await api.openSession(previousKey);
    // An old manually authenticated account must not supply another learner's
    // attempt IDs to the new browser session. Its server history is retained.
    if (previousKey == null) {
      for (final key
          in preferences
              .getKeys()
              .where((k) => k.startsWith('reading.'))
              .toList()) {
        await preferences.remove(key);
      }
    }
    await preferences.setString(
      'reading.visitorKey',
      result['visitorKey'] as String,
    );
    await preferences.setString('reading.jwt', api.token!);
    await preferences.setInt('reading.tenant', result['tenantId'] as int);
    await preferences.setString('reading.username', 'Người học');
    await preferences.setInt(
      'reading.tokenExpires',
      DateTime.now().millisecondsSinceEpoch +
          (result['expireInSeconds'] as int) * 1000,
    );
  }

  Future<void> setActive(String? id) async {
    if (id == null) {
      await preferences.remove('reading.activeAttempt');
    } else {
      await preferences.setString('reading.activeAttempt', id);
    }
  }

  Future<String> requestId(int examId) async {
    final key = 'reading.startRequest.$tenantId.$username.$examId';
    var value = preferences.getString(key);
    if (value == null) {
      final bytes = List.generate(16, (_) => Random.secure().nextInt(256));
      bytes[6] = (bytes[6] & 15) | 64;
      bytes[8] = (bytes[8] & 63) | 128;
      final hex = bytes.map((b) => b.toRadixString(16).padLeft(2, '0')).join();
      value =
          '${hex.substring(0, 8)}-${hex.substring(8, 12)}-${hex.substring(12, 16)}-${hex.substring(16, 20)}-${hex.substring(20)}';
      await preferences.setString(key, value);
    }
    return value;
  }

  Future<void> clearStart(int examId) =>
      preferences.remove('reading.startRequest.$tenantId.$username.$examId');
}

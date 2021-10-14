using System.Collections.Generic;

namespace Microsoft.BotBuilderSamples.Extensions {
  public static class IEnumerable {
    public static IEnumerable<T> ToIEnumerable<T>(this IEnumerator<T> enumerator) {
      while ( enumerator.MoveNext() ) {
        yield return enumerator.Current;
      }
    }
  }
}
using System;

namespace PinionCore.Remote.Tools.Protocol.Sources.TestCommon
{

    public class Spirit1TesterMethodable0 : IMethodable1
    {

        public Spirit1TesterMethodable0() {
            _Value= 0;
        }
        int _Value;
        public PinionCore.Remote.Value<int> GetValue1()
        {
            return _Value;
        }

        internal void SetValue(int testvalue)
        {
            _Value = testvalue;
        }
    }


    public class Spirit1Tester : ISpirit1
    {

        readonly Spirit1TesterMethodable0 methodable0 = new Spirit1TesterMethodable0();
        readonly Spirit1TesterMethodable0 methodable1 = new Spirit1TesterMethodable0();


        readonly Spirit<IMethodable1> spirit1 ;
        readonly Spirit<IMethodable1> spirit2;

        public Spirit1Tester()
        {
            spirit1 = new Spirit<IMethodable1>(methodable0);
            spirit2 = new Spirit<IMethodable1>(methodable1);
        }

        public void TestDispose()
        {
            spirit1.Dispose();
            spirit2.Dispose();
        }

        Spirit<IMethodable1> ISpirit1.Get1()
        {
            return spirit1;
        }

        Spirit<IMethodable1> ISpirit1.Get2(int testvalue)
        {
            methodable1.SetValue(testvalue);
            return spirit2;
        }
    }
}

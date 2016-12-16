using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventStoreIssue
{
    public class DummyEventA
    {
        private readonly Guid id;

        public DummyEventA(Guid id)
        {
            this.id = id;
        }

        public Guid Id => this.id;
    }
}
